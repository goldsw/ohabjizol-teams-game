using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.UI
{
    /// <summary>
    /// Drop on an empty scene object. Builds an overlay canvas and two windows for drag, close, and focus.
    /// </summary>
    public sealed class WindowFrameworkDemo : MonoBehaviour
    {
        void Awake()
        {
            UiKit.EnsureEventSystem();

            var canvasObject = new GameObject("WindowDemoCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            Image background = UiKit.Image(root, "Background", new Color(0.08f, 0.09f, 0.11f, 1f));
            background.raycastTarget = true;
            UiKit.Stretch(background.rectTransform, 0f);

            DemoNoteWindow first = WindowFactory.Create<DemoNoteWindow>(
                root, "메모 1", new Vector2(520f, 360f), new Vector2(-180f, 40f));
            DemoNoteWindow second = WindowFactory.Create<DemoNoteWindow>(
                root, "메모 2", new Vector2(520f, 360f), new Vector2(160f, -50f));
            ReopenButton(root, "Open1", "메모 1 열기", new Vector2(-220f, -460f), first);
            ReopenButton(root, "Open2", "메모 2 열기", new Vector2(220f, -460f), second);
            first.Open();
            second.Open();
        }

        static void ReopenButton(RectTransform parent, string name, string caption, Vector2 position, WindowToolbar window)
        {
            Button button = UiKit.Button(parent, name, caption, new Color(0.20f, 0.32f, 0.42f, 1f), 22);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 64f);
            rect.anchoredPosition = position;
            button.onClick.AddListener(window.Open);
        }

        sealed class DemoNoteWindow : InfoWindow
        {
            protected override void BuildInfo(RectTransform info)
            {
                Text body = UiKit.Text(info, "Body", 24, FontStyle.Normal, TextAnchor.UpperLeft);
                body.color = new Color(0.12f, 0.14f, 0.16f, 1f);
                body.verticalOverflow = VerticalWrapMode.Overflow;
                body.text = Title
                    + "\n\n툴바를 끌어 옮긴다."
                    + "\nX로 닫고, 아래 버튼으로 다시 연다. 자리는 그대로다."
                    + "\n겹친 창을 누르면 앞으로 온다.";
                UiKit.Stretch(body.rectTransform, 20f);
            }
        }
    }
}
