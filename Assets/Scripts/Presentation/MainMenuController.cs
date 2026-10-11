using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Main-menu PLAY button on the monitor. The first press invokes <see cref="onPlay"/>,
    /// which centers the drifted camera. LoadingScene opens only after that move finishes
    /// and the camera is locked. This click path does not zoom. Later presses are ignored.
    /// This does not touch the game managers.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] string loadingSceneName = "LoadingScene";
        [SerializeField] Transform viewRoot;
        [SerializeField] MainMenuCameraController cameraController;
        [SerializeField] Button playButton;
        [SerializeField] Vector2 playButtonAnchoredPosition = new Vector2(0f, -260f);

        /// <summary>
        /// Registered play callback. PLAY invokes this once to center the camera.
        /// The loading scene is not opened when this returns.
        /// </summary>
        public Action onPlay;

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
            BindCanvasCamera();
            onPlay = AlignCenter;
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
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), EventCamera()))
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

            if (onPlay != null)
                onPlay.Invoke();
            else
                OpenAfterCenter();
        }

        void AlignCenter()
        {
            if (cameraController != null)
                cameraController.AlignToCenter(OpenAfterCenter);
            else
                OpenAfterCenter();
        }

        void OpenAfterCenter()
        {
            var camera = cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;
            ComputerTransitionController.RememberPose(camera);
            OpenLoadingScene();
        }

        public Button EnsureWorldPlayButton(ComputerScreen screen)
        {
            if (playButton != null)
                return playButton;

            playButton = CreatePlayButton(screen);
            return playButton;
        }

        void OpenLoadingScene()
        {
            SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
        }

        Camera EventCamera()
        {
            var canvas = playButton != null ? playButton.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            return null;
        }

        void BindCanvasCamera()
        {
            var canvas = playButton != null ? playButton.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                return;

            var camera = cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;
            if (camera != null)
                canvas.worldCamera = camera;
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
            return CreatePlayButton(FindScreen());
        }

        Button CreatePlayButton(ComputerScreen screen)
        {
            var canvasObject = new GameObject("MenuScreenCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.AddComponent<GraphicRaycaster>();

            var root = canvasObject.GetComponent<RectTransform>();
            var camera = cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;
            if (screen != null)
                screen.PlaceWorldCanvas(root, camera);
            else
                Debug.LogError("Menu UI needs a ComputerScreen so the canvas can sit on the monitor.");

            var title = CreateText(canvasObject.transform, "Title", "MAIN MENU", 220, FontStyle.Bold);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 190f);
            titleRect.sizeDelta = new Vector2(1500f, 280f);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;

            var buttonObject = new GameObject("PlayButton");
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = playButtonAnchoredPosition;
            buttonRect.sizeDelta = new Vector2(1100f, 220f);

            var image = buttonObject.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = new Color(0.12f, 0.38f, 0.28f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(buttonObject.transform, "Label", "PLAY", 120, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(label.rectTransform);
            return button;
        }

        ComputerScreen FindScreen()
        {
            if (viewRoot != null)
            {
                var onView = viewRoot.GetComponentInChildren<ComputerScreen>(true);
                if (onView != null)
                    return onView;
            }

            return FindFirstObjectByType<ComputerScreen>();
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
