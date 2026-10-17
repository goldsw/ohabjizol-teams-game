using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Desktop palette and top-left pixel placement shared by the desktop windows.
    /// </summary>
    static class DesktopUi
    {
        public static readonly Color DiscordDark = new Color32(0x1E, 0x1F, 0x22, 0xFF);
        public static readonly Color DiscordPanel = new Color32(0x2B, 0x2D, 0x31, 0xFF);
        public static readonly Color DiscordMain = new Color32(0x31, 0x33, 0x38, 0xFF);
        public static readonly Color DiscordAccent = new Color32(0x58, 0x65, 0xF2, 0xFF);
        public static readonly Color DiscordText = new Color32(0xDB, 0xDE, 0xE1, 0xFF);
        public static readonly Color DiscordMuted = new Color32(0x94, 0x9B, 0xA4, 0xFF);
        public static readonly Color DiscordGreen = new Color32(0x23, 0xA5, 0x5A, 0xFF);
        public static readonly Color DangerRed = new Color32(0xDA, 0x37, 0x3C, 0xFF);
        public static readonly Color WarnOrange = new Color32(0xC2, 0x7C, 0x0E, 0xFF);
        public static readonly Color NeutralButton = new Color32(0x4E, 0x50, 0x58, 0xFF);

        public static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        public static Text Label(Transform parent, string name, string value, int size, FontStyle style, TextAnchor align, Color color)
        {
            Text text = UiKit.Text(parent, name, size, style, align);
            text.text = value;
            text.color = color;
            return text;
        }

        public static Text ButtonLabel(Button button)
        {
            return button != null ? button.GetComponentInChildren<Text>(true) : null;
        }
    }
}
