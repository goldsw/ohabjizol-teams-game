#if UNITY_EDITOR
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace Ohabjizol.Interview
{
    // Used only by explicit editor baking; no runtime UI construction.
    internal static class ResultAuthoredUi
    {
        internal static Font Font => UiKit.Font;
        internal static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        internal static RectTransform Canvas(Transform parent)
        {
            var root = Rect(parent, "AuthoredUi", Vector2.zero, Vector2.one);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.gameObject.AddComponent<GraphicRaycaster>();
            var background = Rect(root, "OrangeBackground", Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.46f, 0.12f);
            return root;
        }
        internal static Text Label(Transform parent, string name, string caption, int size, Vector2 min, Vector2 max)
        {
            var rect = Rect(parent, name, min, max);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.text = caption;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.10f, 0.06f);
            text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
        internal static Button Button(Transform parent, string name, string caption, out Text label)
        {
            var rect = Rect(parent, name, new Vector2(.35f, .10f), new Vector2(.65f, .20f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, .86f, .61f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            label = Label(rect, "Caption", caption, 36, Vector2.zero, Vector2.one);
            return button;
        }
    }
}
#endif
