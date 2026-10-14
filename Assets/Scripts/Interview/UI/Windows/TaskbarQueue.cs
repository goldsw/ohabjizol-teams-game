using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class TaskbarQueue : IDisposable
    {
        Queue<IManagedWindow> windows = new Queue<IManagedWindow>();
        bool disposed;
        public IManagedWindow ActiveWindow { get; private set; }
        public IReadOnlyList<IManagedWindow> Windows => Array.AsReadOnly(windows.ToArray());
        public event Action Changed;

        public bool TryGetWindow(DocumentKey key, out IManagedWindow window)
        {
            foreach (var entry in windows)
            {
                if (!entry.IsClosed && entry.DocumentKey.Equals(key))
                {
                    window = entry;
                    return true;
                }
            }
            window = null;
            return false;
        }

        public void Register(IManagedWindow window)
        {
            if (disposed) throw new ObjectDisposedException(nameof(TaskbarQueue));
            if (window == null || window.IsClosed || windows.Contains(window)) return;
            if (TryGetWindow(window.DocumentKey, out var existing))
                throw new InvalidOperationException("A window for this document is already registered.");
            windows.Enqueue(window);
            window.StateChanged += OnStateChanged;
            window.Closed += Unregister;
            window.FocusRequested += Focus;
            Changed?.Invoke();
        }

        public void Unregister(IManagedWindow window)
        {
            if (window == null || !windows.Contains(window)) return;
            window.StateChanged -= OnStateChanged;
            window.Closed -= Unregister;
            window.FocusRequested -= Focus;
            var remaining = new Queue<IManagedWindow>();
            foreach (var entry in windows)
                if (!ReferenceEquals(entry, window)) remaining.Enqueue(entry);
            windows = remaining;
            if (ReferenceEquals(ActiveWindow, window)) ActiveWindow = null;
            Changed?.Invoke();
        }

        public void Focus(IManagedWindow window)
        {
            if (disposed || window == null || window.IsClosed ||
                !window.IsVisible || !windows.Contains(window)) return;
            if (ReferenceEquals(ActiveWindow, window)) return;
            ActiveWindow = window;
            Changed?.Invoke();
        }

        public void ClearFocus()
        {
            if (ActiveWindow == null) return;
            ActiveWindow = null;
            Changed?.Invoke();
        }

        void OnStateChanged(IManagedWindow window)
        {
            if (window.IsClosed) { Unregister(window); return; }
            if (ReferenceEquals(ActiveWindow, window) && !window.IsVisible)
                ActiveWindow = null;
            Changed?.Invoke();
        }

        public void CloseAll()
        {
            foreach (var window in windows.ToArray())
            {
                window.Close();
                Unregister(window);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var window in windows.ToArray()) Unregister(window);
            Changed = null;
        }
    }
}
