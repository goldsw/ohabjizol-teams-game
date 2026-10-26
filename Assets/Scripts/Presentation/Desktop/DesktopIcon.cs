using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Fixed desktop shortcut. A click selects it; a second click within
    /// <see cref="DoubleClickSeconds"/> activates it. Timing is measured here as well as read
    /// from <see cref="PointerEventData.clickCount"/>, which may stay at 1 on the world canvas.
    /// </summary>
    public sealed class DesktopIcon : MonoBehaviour, IPointerClickHandler
    {
        public const float DoubleClickSeconds = 0.35f;

        static readonly Color SelectedFill = new Color(0.55f, 0.70f, 1f, 0.32f);
        static readonly Color LabelColor = new Color(0.97f, 0.97f, 0.98f, 1f);

        [SerializeField] Image _highlight;
        float _lastClick = float.NegativeInfinity;
        bool _selected;

        public event Action<DesktopIcon> Clicked;

        public event Action<DesktopIcon> Activated;

        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (_highlight != null)
                    _highlight.color = value ? SelectedFill : Color.clear;
                if (!value)
                    _lastClick = float.NegativeInfinity;
            }
        }

        public static DesktopIcon Create(RectTransform parent, string name, string label, string glyph, Color tileColor, DesktopLayout layout, int index)
        {
            Image cell = UiKit.Image(parent, name, Color.clear);
            cell.raycastTarget = true;
            DesktopUi.PlaceTopLeft(
                cell.rectTransform,
                layout.IconOrigin.x,
                layout.IconOrigin.y + index * layout.IconSpacing,
                layout.IconCell.x,
                layout.IconCell.y);

            Image tile = UiKit.Image(cell.transform, "Tile", tileColor);
            RectTransform tileRect = tile.rectTransform;
            tileRect.anchorMin = new Vector2(0.5f, 1f);
            tileRect.anchorMax = new Vector2(0.5f, 1f);
            tileRect.pivot = new Vector2(0.5f, 1f);
            tileRect.sizeDelta = new Vector2(layout.IconTile, layout.IconTile);
            tileRect.anchoredPosition = new Vector2(0f, -8f);

            Text glyphText = DesktopUi.Label(tile.transform, "Glyph", glyph, Mathf.RoundToInt(layout.IconTile * 0.5f), FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            UiKit.Stretch(glyphText.rectTransform, 0f);
            glyphText.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text caption = DesktopUi.Label(cell.transform, "Label", label, 20, FontStyle.Normal, TextAnchor.UpperCenter, LabelColor);
            RectTransform captionRect = caption.rectTransform;
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(1f, 1f);
            captionRect.offsetMin = new Vector2(2f, 2f);
            captionRect.offsetMax = new Vector2(-2f, -(layout.IconTile + 14f));
            Shadow shadow = caption.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            DesktopIcon icon = cell.gameObject.AddComponent<DesktopIcon>();
            icon._highlight = cell;
            return icon;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (Clicked != null || Activated != null)
                Ohabjizol.Presentation.UiSoundPlayer.PlayClick();
            float now = Time.unscaledTime;
            bool twice = eventData.clickCount >= 2 || now - _lastClick <= DoubleClickSeconds;
            _lastClick = twice ? float.NegativeInfinity : now;

            if (Clicked != null)
                Clicked(this);
            if (twice && Activated != null)
                Activated(this);
        }
    }
}
