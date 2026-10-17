using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Draws the boot UI on this scene's monitor and async-loads the next scene.
    /// The camera stays on the centered pose PLAY already locked. This scene does not
    /// center again. When loading finishes, the screen shows <see cref="completeText"/>,
    /// then the camera zooms in, then the next scene opens.
    /// The displayed gauge is not <see cref="AsyncOperation.progress"/>, which stops at 0.9
    /// until activation.
    /// </summary>
    public sealed class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] float minimumLoadingDuration = 1.75f;
        [SerializeField] float loadingProgressSpeed = 0.55f;
        [SerializeField] string nextSceneName = "InterviewScene";
        [SerializeField] ComputerTransitionController transition;
        [SerializeField] RectTransform loadingUiOverride;
        [SerializeField] string bootText = "청강코드 실행 중…";
        [SerializeField] string waitText = "서버에 연결하는 중…";
        [SerializeField] string completeText = "연결 완료";
        [SerializeField] float completeHoldDuration = 0.75f;
        [SerializeField] bool showPercent = true;
        [SerializeField] float labelPulseSpeed = 2.2f;
        [SerializeField] Color backgroundColor = new Color32(0x31, 0x33, 0x38, 0xFF);
        [SerializeField] Color barColor = new Color32(0x1E, 0x1F, 0x22, 0xFF);
        [SerializeField] Color accentColor = new Color32(0x58, 0x65, 0xF2, 0xFF);
        [SerializeField] Color textColor = new Color32(0xF2, 0xF3, 0xF5, 0xFF);
        [SerializeField] Color mutedTextColor = new Color32(0xB5, 0xBA, 0xC1, 0xFF);

        AsyncOperation _operation;
        float _elapsed;
        float _displayedProgress;
        bool _completeShown;
        float _completeElapsed;
        bool _zoomStarted;
        bool _activated;
        Text _bootLabel;
        Text _waitLabel;
        Text _percentLabel;
        Image _progressFill;
        RectTransform _scan;
        static string _pendingScene;
        static Sprite _whiteSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLoadHook()
        {
            SceneManager.sceneLoaded -= HandleInterviewLoaded;
            _pendingScene = null;
        }

        void Awake()
        {
            if (transition == null || !transition.IsRunning)
                ApplyFixedCameraPose();
        }

        void Start()
        {
            BuildLoadingUi();
            _operation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
            if (_operation == null)
            {
                Debug.LogError("Could not start async load for " + nextSceneName);
                return;
            }

            _operation.allowSceneActivation = false;
        }

        void Update()
        {
            if (_operation == null || _activated || _zoomStarted)
                return;

            if (!_completeShown)
            {
                _elapsed += Time.deltaTime;
                bool loadReady = _operation.progress >= 0.9f;
                bool timeReady = _elapsed >= minimumLoadingDuration;
                float asyncPortion = Mathf.Clamp01(_operation.progress / 0.9f);
                float visualTarget = loadReady ? 0.92f : asyncPortion * 0.92f;
                if (loadReady && timeReady)
                    visualTarget = 1f;

                if (loadingProgressSpeed <= 0f)
                    _displayedProgress = visualTarget;
                else
                    _displayedProgress = Mathf.MoveTowards(_displayedProgress, visualTarget, loadingProgressSpeed * Time.deltaTime);

                UpdateLoadingUi();
                if (!loadReady || !timeReady || _displayedProgress < 0.999f)
                    return;

                ShowComplete();
                return;
            }

            _completeElapsed += Time.deltaTime;
            if (completeHoldDuration > 0f && _completeElapsed < completeHoldDuration)
                return;

            _zoomStarted = true;
            if (transition == null)
                transition = FindFirstObjectByType<ComputerTransitionController>();
            if (transition != null)
                transition.Begin(ActivateInterview);
            else
                ActivateInterview();
        }

        void ShowComplete()
        {
            _completeShown = true;
            _displayedProgress = 1f;
            if (_progressFill != null)
                _progressFill.fillAmount = 1f;
            if (_percentLabel != null && showPercent)
                _percentLabel.text = "100%";
            if (_bootLabel != null)
            {
                _bootLabel.text = completeText;
                var color = _bootLabel.color;
                color.a = 1f;
                _bootLabel.color = color;
            }

            if (_waitLabel != null)
                _waitLabel.text = string.Empty;
        }

        public RectTransform CreateLoadingCanvas(ComputerScreen screen)
        {
            if (screen == null)
                return loadingUiOverride;

            if (loadingUiOverride == null)
                loadingUiOverride = CreateDefaultUi(screen.transform);
            else
                screen.PlaceWorldCanvas(loadingUiOverride, null);
            return loadingUiOverride;
        }

        void ApplyFixedCameraPose()
        {
            var camera = Camera.main;
            if (camera == null || camera.gameObject.scene != gameObject.scene)
            {
                camera = null;
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    camera = root.GetComponentInChildren<Camera>(true);
                    if (camera != null)
                        break;
                }
            }

            ComputerTransitionController.ApplyFixedPose(camera);
        }

        void OnDestroy()
        {
            if (!_activated)
                SceneManager.sceneLoaded -= HandleInterviewLoaded;
        }

        void ActivateInterview()
        {
            if (_activated || _operation == null)
                return;

            _activated = true;
            _pendingScene = nextSceneName;
            SceneManager.sceneLoaded += HandleInterviewLoaded;
            _operation.allowSceneActivation = true;
        }

        static void HandleInterviewLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(_pendingScene) || scene.name != _pendingScene)
                return;

            SceneManager.sceneLoaded -= HandleInterviewLoaded;
            _pendingScene = null;

            var screen = FindFirstObjectByType<ComputerScreen>();
            if (screen != null && screen.ViewRoot != null)
                Destroy(screen.ViewRoot.gameObject);
        }

        void BuildLoadingUi()
        {
            var screen = FindScreen();
            if (screen == null)
            {
                Debug.LogError("Loading UI needs a ComputerScreen on this scene's monitor.");
                return;
            }

            RectTransform root = loadingUiOverride;
            if (root == null)
                root = CreateDefaultUi(screen.transform);
            else
                PlaceOnScreen(root, screen.transform);

            _bootLabel = FindText(root, "BootText");
            _waitLabel = FindText(root, "WaitText");
            _percentLabel = FindText(root, "PercentText");
            var fill = root.Find("ProgressFill");
            if (fill == null)
            {
                var bar = root.Find("ProgressBar");
                if (bar != null)
                    fill = bar.Find("ProgressFill");
            }

            if (fill != null)
                _progressFill = fill.GetComponent<Image>();
            var scan = root.Find("ScanLine");
            if (scan != null)
                _scan = scan as RectTransform;

            ApplyTheme(root);
            if (_bootLabel != null)
                _bootLabel.text = bootText;
            if (_waitLabel != null)
                _waitLabel.text = waitText;
            if (_percentLabel != null)
                _percentLabel.gameObject.SetActive(showPercent);
        }

        /// <summary>
        /// Recolors the scene-saved or default UI by object name. Labels switch to
        /// <see cref="UiKit.Font"/> because the built-in font has no Hangul.
        /// </summary>
        void ApplyTheme(RectTransform root)
        {
            var background = root.Find("Background");
            if (background != null && background.GetComponent<Image>() != null)
                background.GetComponent<Image>().color = backgroundColor;

            var bar = root.Find("ProgressBar");
            if (bar != null && bar.GetComponent<Image>() != null)
                bar.GetComponent<Image>().color = barColor;
            if (_progressFill != null)
                _progressFill.color = accentColor;
            if (_scan != null && _scan.GetComponent<Image>() != null)
                _scan.GetComponent<Image>().color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f);

            StyleLabel(_bootLabel, textColor);
            StyleLabel(_waitLabel, mutedTextColor);
            StyleLabel(_percentLabel, mutedTextColor);
        }

        static void StyleLabel(Text label, Color color)
        {
            if (label == null)
                return;

            label.font = UiKit.Font;
            label.color = color;
        }

        void UpdateLoadingUi()
        {
            if (_progressFill != null)
                _progressFill.fillAmount = _displayedProgress;
            if (_percentLabel != null && showPercent)
                _percentLabel.text = Mathf.RoundToInt(_displayedProgress * 100f) + "%";
            if (_bootLabel != null && labelPulseSpeed > 0f)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * labelPulseSpeed);
                var color = _bootLabel.color;
                color.a = pulse;
                _bootLabel.color = color;
            }

            if (_scan != null && labelPulseSpeed > 0f)
            {
                float y = Mathf.Lerp(-360f, 360f, Mathf.PingPong(Time.time * 0.35f, 1f));
                _scan.anchoredPosition = new Vector2(0f, y);
            }
        }

        ComputerScreen FindScreen()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                var screen = root.GetComponentInChildren<ComputerScreen>(true);
                if (screen != null)
                    return screen;
            }

            return FindFirstObjectByType<ComputerScreen>();
        }

        static RectTransform CreateDefaultUi(Transform screen)
        {
            var rootObject = new GameObject("LoadingUI");
            var root = rootObject.AddComponent<RectTransform>();
            var canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            rootObject.AddComponent<GraphicRaycaster>();
            PlaceOnScreen(root, screen);

            var background = CreateImage(root, "Background", new Color(0.01f, 0.04f, 0.035f, 1f));
            Stretch(background.rectTransform);

            var boot = CreateLabel(root, "BootText", "SYSTEM BOOTING…", 150);
            boot.rectTransform.anchoredPosition = new Vector2(0f, 230f);
            boot.rectTransform.sizeDelta = new Vector2(1580f, 240f);

            var wait = CreateLabel(root, "WaitText", "PLEASE WAIT…", 72);
            wait.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            wait.rectTransform.sizeDelta = new Vector2(1500f, 120f);

            var bar = CreateImage(root, "ProgressBar", new Color(0.05f, 0.12f, 0.09f, 1f));
            bar.rectTransform.anchoredPosition = new Vector2(0f, -155f);
            bar.rectTransform.sizeDelta = new Vector2(1460f, 84f);

            var fill = CreateImage(bar.transform, "ProgressFill", new Color(0.35f, 0.95f, 0.62f, 1f));
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            var percent = CreateLabel(root, "PercentText", "0%", 64);
            percent.rectTransform.anchoredPosition = new Vector2(0f, -310f);
            percent.rectTransform.sizeDelta = new Vector2(800f, 110f);

            var scan = CreateImage(root, "ScanLine", new Color(0.5f, 1f, 0.75f, 0.18f));
            scan.rectTransform.sizeDelta = new Vector2(1580f, 20f);
            return root;
        }

        static void PlaceOnScreen(RectTransform root, Transform screen)
        {
            var marker = screen.GetComponent<ComputerScreen>();
            if (marker != null)
            {
                marker.PlaceWorldCanvas(root, Camera.main);
                return;
            }

            root.SetParent(screen, false);
            root.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.localPosition = Vector3.zero;
            root.sizeDelta = new Vector2(5000f, 3000f);
            root.localScale = new Vector3(1f / 5000f, 1f / 3000f, 1f / 5000f);
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.pixelPerfect = false;
                canvas.updateRectTransformForStandalone = StandaloneRenderResize.Disabled;
                canvas.worldCamera = Camera.main;
            }
        }

        static Text FindText(Transform root, string name)
        {
            var child = root.Find(name);
            if (child == null)
                return null;
            return child.GetComponent<Text>();
        }

        static Text CreateLabel(Transform parent, string name, string value, int size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.55f, 1f, 0.72f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            imageObject.AddComponent<RectTransform>();
            var image = imageObject.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
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
