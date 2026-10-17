using UnityEngine;

namespace Ohabjizol.UI
{
    /// <summary>
    /// Creates a window object, adds the component, and builds it once.
    /// </summary>
    public static class WindowFactory
    {
        public static T Instantiate<T>(T prefab, RectTransform parent) where T : WindowToolbar
        {
            if (prefab == null || parent == null)
                throw new System.ArgumentException("Window prefab and parent must be assigned.");
            T window = Object.Instantiate(prefab, parent, false);
            window.InitializeAuthored();
            return window;
        }

        public static T Create<T>(RectTransform parent, string title, Vector2 size, Vector2 anchoredPosition)
            where T : WindowToolbar
        {
            if (!Application.isEditor || Application.isPlaying)
                throw new System.InvalidOperationException("Create is only available for explicit Editor baking.");
            var host = new GameObject(typeof(T).Name);
            host.SetActive(false);
            T window = host.AddComponent<T>();
            window.Build(parent, title, size, anchoredPosition);
            return window;
        }
    }
}
