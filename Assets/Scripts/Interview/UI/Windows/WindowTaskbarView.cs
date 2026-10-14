using System;
using System.Collections.Generic;
using Ohabjizol.UI;
using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class WindowTaskbarView : MonoBehaviour, IDisposable
    {
        [SerializeField] RectTransform container;
        [SerializeField] ManagedWindowTaskbarItem itemPrefab;
        [SerializeField] Transform focusRoot;
        readonly TaskbarQueue queue = new TaskbarQueue();
        readonly Dictionary<IManagedWindow, ManagedWindowTaskbarItem> items = new Dictionary<IManagedWindow, ManagedWindowTaskbarItem>();
        readonly List<WindowToolbar> toolbars = new List<WindowToolbar>();
        bool subscribed;
        public TaskbarQueue Queue => queue;
        public void ConfigureEditor(RectTransform host, ManagedWindowTaskbarItem prefab, Transform root)
        { Detach(); container = host; itemPrefab = prefab; focusRoot = root; if (isActiveAndEnabled) Attach(); }
        void OnEnable() { Attach(); }
        void OnDisable() { Detach(); }
        void Attach()
        {
            if (subscribed) return;
            queue.Changed += Refresh;
            subscribed = true;
            if (focusRoot)
                foreach (WindowToolbar toolbar in focusRoot.GetComponentsInChildren<WindowToolbar>(true))
                    if (!(toolbar is IManagedWindow))
                    { toolbar.Foregrounded += Foregrounded; toolbars.Add(toolbar); }
            Refresh();
        }
        void Foregrounded(WindowToolbar toolbar) { queue.ClearFocus(); }
        void Detach()
        {
            if (subscribed) queue.Changed -= Refresh;
            subscribed = false;
            foreach (WindowToolbar toolbar in toolbars) if (toolbar) toolbar.Foregrounded -= Foregrounded;
            toolbars.Clear();
        }
        void Refresh()
        {
            if (!container || !itemPrefab) return;
            var remaining = new HashSet<IManagedWindow>(queue.Windows);
            var removed = new List<IManagedWindow>();
            foreach (var pair in items) if (!remaining.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (IManagedWindow window in removed)
            {
                if (items[window]) { items[window].Dispose(); items[window].gameObject.SetActive(false); Destroy(items[window].gameObject); }
                items.Remove(window);
            }
            int index = 0;
            foreach (IManagedWindow window in queue.Windows)
            {
                if (!items.TryGetValue(window, out ManagedWindowTaskbarItem item) || !item)
                { item = Instantiate(itemPrefab, container, false); item.gameObject.SetActive(true); item.Bind(window); items[window] = item; }
                item.transform.SetSiblingIndex(index++);
                item.Refresh();
            }
        }
        public void Dispose()
        {
            Detach();
            foreach (ManagedWindowTaskbarItem item in items.Values) if (item) { item.Dispose(); Destroy(item.gameObject); }
            items.Clear();
        }
        void OnDestroy() { Dispose(); }
    }
}
