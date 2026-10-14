using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class ManagedWindowTaskbarItem : MonoBehaviour, IDisposable
    {
        [SerializeField] Button button;
        [SerializeField] Text title;
        [SerializeField] Text iconText;
        [SerializeField] Image icon;
        [SerializeField] Image background;
        IManagedWindow window;

        public void ConfigureEditor(Button click, Text label, Text glyph, Image sprite, Image surface)
        { button = click; title = label; iconText = glyph; icon = sprite; background = surface; }
        public void Bind(IManagedWindow value)
        {
            Dispose();
            window = value;
            WindowTaskbarDescriptor descriptor = value.TaskbarInfo;
            if (title) title.text = descriptor.Title;
            if (iconText) { iconText.text = descriptor.IconText; iconText.gameObject.SetActive(descriptor.Icon == null); }
            if (icon) { icon.sprite = descriptor.Icon; icon.gameObject.SetActive(descriptor.Icon != null); }
            if (button) button.onClick.AddListener(Click);
            Refresh();
        }
        public void Refresh()
        {
            if (background) background.color = window != null && window.IsActive
                ? new Color32(255, 174, 76, 255) : new Color32(166, 159, 152, 255);
        }
        void Click()
        {
            if (window == null || window.IsClosed) return;
            if (window.IsActive) window.Minimize(); else window.Maximize();
        }
        public void Dispose() { if (button) button.onClick.RemoveListener(Click); window = null; }
        void OnDestroy() { Dispose(); }
    }
}
