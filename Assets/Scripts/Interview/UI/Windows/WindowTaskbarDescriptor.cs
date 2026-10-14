using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class WindowTaskbarDescriptor
    {
        public string Title { get; }
        public string IconText { get; }
        public Sprite Icon { get; }

        public WindowTaskbarDescriptor(string title, string iconText = "PDF", Sprite icon = null)
        {
            Title = title ?? string.Empty;
            IconText = iconText ?? string.Empty;
            Icon = icon;
        }
    }
}
