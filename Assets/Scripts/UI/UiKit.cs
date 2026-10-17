using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Ohabjizol.UI
{
    /// <summary>
    /// Shared text, image, and button builders. Game namespaces stay out of this type.
    /// </summary>
    public static class UiKit
    {
        static readonly Color PaperColor = new Color(0.93f, 0.95f, 0.92f, 1f);

        static Font _font;
        static Sprite _white;

        public static Font Font
        {
            get
            {
                if (_font != null)
                    return _font;

                _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim" }, 40);
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Sprite White
        {
            get
            {
                if (_white != null)
                    return _white;

                Texture2D texture = Texture2D.whiteTexture;
                _white = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                return _white;
            }
        }

        public static Text Text(Transform parent, string name, int size, FontStyle style, TextAnchor align)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var text = textObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.color = PaperColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            return text;
        }

        public static Image Image(Transform parent, string name, Color color)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var image = imageObject.AddComponent<Image>();
            image.sprite = White;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Button Button(Transform parent, string name, string caption, Color color, int size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var image = buttonObject.AddComponent<Image>();
            image.sprite = White;
            image.color = Color.white;
            image.raycastTarget = true;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            Paint(button, color);
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            buttonObject.AddComponent<WindowPointerFocus>();

            Text label = Text(buttonObject.transform, "Label", size, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.text = caption ?? string.Empty;
            label.color = PaperColor;
            Stretch(label.rectTransform, 8f);
            return button;
        }

        /// <summary>
        /// Same color block as the interview screen: white highlight, dark press, dim disabled.
        /// </summary>
        public static void Paint(Button button, Color color)
        {
            if (button == null)
                return;

            ColorBlock block = button.colors;
            block.normalColor = color;
            block.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
            block.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            block.selectedColor = color;
            block.disabledColor = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 0.65f);
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.05f;
            button.colors = block;
        }

        public static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void Stretch(RectTransform rect, float padding)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            var module = eventObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
