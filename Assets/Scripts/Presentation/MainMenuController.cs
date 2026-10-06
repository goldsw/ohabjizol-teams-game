using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Main-menu PLAY button. The first click locks input and loads the loading scene.
    /// Later clicks are ignored. This does not touch the game managers.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] string loadingSceneName = "LoadingScene";
        [SerializeField] Transform viewRoot;
        [SerializeField] MainMenuCameraController cameraController;
        [SerializeField] Button playButton;

        bool _accepted;
        static Sprite _whiteSprite;

        void Awake()
        {
            if (cameraController == null)
                cameraController = FindFirstObjectByType<MainMenuCameraController>();
            if (viewRoot == null)
            {
                var screen = FindFirstObjectByType<ComputerScreen>();
                if (screen != null)
                    viewRoot = screen.ViewRoot;
            }

            EnsureEventSystem();
            if (playButton == null)
                playButton = CreatePlayButton();
            playButton.onClick.AddListener(Play);
        }

        void Update()
        {
            if (_accepted || playButton == null || Mouse.current == null)
                return;
            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            var rect = playButton.transform as RectTransform;
            if (rect == null)
                return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), null))
                return;

            Play();
        }

        public void Play()
        {
            if (_accepted)
                return;

            _accepted = true;
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
                playButton.interactable = false;
            }

            if (cameraController != null)
                cameraController.LockLook();

            if (viewRoot != null)
            {
                viewRoot.SetParent(null, true);
                DontDestroyOnLoad(viewRoot.gameObject);
            }

            SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Single);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            var module = eventObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        Button CreatePlayButton()
        {
            var canvasObject = new GameObject("MainMenuCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var title = CreateText(canvasObject.transform, "Title", "MAIN MENU", 42, FontStyle.Bold);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 160f);
            titleRect.sizeDelta = new Vector2(800f, 80f);

            var buttonObject = new GameObject("PlayButton");
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, 40f);
            buttonRect.sizeDelta = new Vector2(320f, 76f);

            var image = buttonObject.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = new Color(0.12f, 0.38f, 0.28f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(buttonObject.transform, "Label", "PLAY", 32, FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        static Text CreateText(Transform parent, string name, string value, int size, FontStyle style)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.86f, 0.95f, 0.88f, 1f);
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var texture = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _whiteSprite;
        }
    }
}
