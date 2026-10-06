using System;

namespace Ohabjizol.Managers
{
    public enum WindowMode
    {
        Windowed,
        Borderless,
        Fullscreen
    }

    /// <summary>
    /// Applies resolution and window mode. Settings stores the choice and does not call a display API.
    /// This type records the applied value and notifies listeners. A later Unity adapter performs the OS change.
    /// </summary>
    public sealed class WindowManager
    {
        public bool HasAppliedResolution { get; private set; }

        public Resolution AppliedResolution { get; private set; }

        public bool HasAppliedWindowMode { get; private set; }

        public WindowMode AppliedWindowMode { get; private set; }

        public event Action<Resolution> ResolutionApplied;

        public event Action<WindowMode> WindowModeApplied;

        public void ApplyResolution(Resolution resolution)
        {
            AppliedResolution = resolution;
            HasAppliedResolution = true;

            var handler = ResolutionApplied;
            if (handler != null)
                handler(resolution);
        }

        public void ApplyWindowMode(WindowMode mode)
        {
            if (!Enum.IsDefined(typeof(WindowMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode), "Unknown window mode.");

            AppliedWindowMode = mode;
            HasAppliedWindowMode = true;

            var handler = WindowModeApplied;
            if (handler != null)
                handler(mode);
        }
    }
}
