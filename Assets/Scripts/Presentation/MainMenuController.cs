using System;
using System.Collections;
using Ohabjizol.Interview;
using Ohabjizol.Tutorial;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    [DefaultExecutionOrder(-1000)]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] string interviewSceneName = "InterviewScene";
        [SerializeField] string endingSceneName = "EndingScene";
        [SerializeField] Transform viewRoot;
        [SerializeField] MainMenuCameraController cameraController;
        [SerializeField] float connectDelay = 0f;
        [SerializeField] DesktopLayout desktopLayout = new DesktopLayout();
        [SerializeField] DesktopInitialState initialWindows = new DesktopInitialState();
        [SerializeField] DesktopView _desktop;
        [SerializeField] RectTransform _generatedDesktopRoot;
        [SerializeField] GameObject bootOverlay;
        [SerializeField] Transform introStart;
        [SerializeField] Transform zoomTarget;
        [SerializeField] MonitorClickHandler monitorClick;
        [SerializeField] float introMoveDuration = 1f;
        [SerializeField] float bootDuration = 1.5f;
        [SerializeField] Image bootProgressImage;
        [SerializeField] Text bootProgressText;
        [SerializeField] float minimumLoadingDuration = 1.5f;
        [SerializeField] float loadingProgressSpeed = 0.5f;
        [SerializeField] float loadingCompleteHold = 0.5f;
        [SerializeField] ComputerTransitionController transition;
        [SerializeField] LoadingUiPresenter loadingPresenter;
        [SerializeField] bool forceTutorialOnTitleStart = true;
        public Action onPlay;
        [SerializeField] PhysicsRaycaster monitorPhysicsRaycaster;
        bool _monitorPickingCaptured, _monitorOriginallyEnabled;
        MenuPhase _phase;
        bool _accepted;
        bool _alignmentCompleted, _zoomStarted, _activationRequested, _callbacksAllowed;
        SceneLaunchFlow _launchFlow;
        string _launchSceneName;
        TutorialWindow _tutorialWindow;
        bool _tutorialPromptOpen;
        bool _titleWasBlocking;
        bool _tutorialForced;
        static bool _introCompleted;
        public enum MenuPhase { MainMenu3D = 0, Zooming = 1, Booting = 2, DesktopReady = 3, Launching = 4, Loading = 5, FillZoom = 6, Activating = 7 }
        public MenuPhase Phase
        {
            get => _phase;
            private set { _phase = value; ApplyMonitorPicking(); }
        }
        public bool HasAuthoredUi => _desktop != null && _desktop.HasAuthoredUi &&
            _generatedDesktopRoot != null && bootOverlay != null && introStart != null &&
            zoomTarget != null && cameraController != null && monitorClick != null &&
            bootProgressImage != null && bootProgressText != null && transition != null && loadingPresenter != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { _introCompleted = false; }

        void Awake()
        {
            if (!Application.isPlaying) return;
            bool tutorialReturn = TutorialSceneFlow.ConsumeReturn();
            if (tutorialReturn) MainMenuTitleOverlay.SuppressForSession();
            if (!HasAuthoredUi)
            {
                Debug.LogError("MainMenu requires authored Desktop, camera poses, monitor handler, boot progress Image/Text, ComputerTransitionController and LoadingUiPresenter references. Run editor authoring and save before Play.", this);
                enabled = false;
                return;
            }
            if (!loadingPresenter.Bind(out var presenterError))
            {
                Debug.LogError(presenterError, this);
                enabled = false;
                return;
            }
            if (!_desktop.Bind(connectDelay, Launch)) { enabled = false; return; }
            _tutorialWindow = _desktop.Tutorial;
            SubscribeTutorial();
            monitorClick.Bind(this);
            _callbacksAllowed = true;
            _launchFlow = new SceneLaunchFlow(minimumLoadingDuration, loadingProgressSpeed, loadingCompleteHold);
            onPlay = AlignCenter;
            bootOverlay.SetActive(false);
            loadingPresenter.Hide();
            _generatedDesktopRoot.gameObject.SetActive(false);
            if (_introCompleted || tutorialReturn)
            {
                cameraController.SetAuthoredPose(zoomTarget);
                Ready();
            }
            else
            {
                cameraController.SetAuthoredPose(introStart);
                Phase = MenuPhase.MainMenu3D;
            }
            _titleWasBlocking = MainMenuTitleOverlay.IsBlocking;
            cameraController.SetLookEnabled(!_titleWasBlocking);
        }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            SubscribeTutorial();
            MainMenuTitleOverlay.Dismissed -= ForceTutorial;
            MainMenuTitleOverlay.Dismissed += ForceTutorial;
            if (!monitorPhysicsRaycaster)
            {
                var cam = MenuCamera() ?? Camera.main;
                if (cam) monitorPhysicsRaycaster = cam.GetComponent<PhysicsRaycaster>();
            }
            if (monitorPhysicsRaycaster && !_monitorPickingCaptured)
            {
                _monitorOriginallyEnabled = monitorPhysicsRaycaster.enabled;
                _monitorPickingCaptured = true;
            }
            ApplyMonitorPicking();
        }

        void ApplyMonitorPicking()
        {
            if (_monitorPickingCaptured && monitorPhysicsRaycaster)
                monitorPhysicsRaycaster.enabled = _monitorOriginallyEnabled && enabled &&
                    Phase == MenuPhase.MainMenu3D && !MainMenuTitleOverlay.IsBlocking;
        }

        void RestoreMonitorPicking()
        {
            if (_monitorPickingCaptured && monitorPhysicsRaycaster)
                monitorPhysicsRaycaster.enabled = _monitorOriginallyEnabled;
            _monitorPickingCaptured = false;
        }

        void OnDisable()
        {
            UnsubscribeTutorial();
            MainMenuTitleOverlay.Dismissed -= ForceTutorial;
            _callbacksAllowed = false;
            StopAllCoroutines();
            RestoreMonitorPicking();
        }
        void OnDestroy()
        {
            UnsubscribeTutorial();
            MainMenuTitleOverlay.Dismissed -= ForceTutorial;
            _callbacksAllowed = false;
            RestoreMonitorPicking();
        }

        // Title start skips the monitor click and runs the boot, then launches the tutorial from the desktop.
        void ForceTutorial()
        {
            if (!forceTutorialOnTitleStart || !enabled || Phase != MenuPhase.MainMenu3D) return;
            _tutorialForced = true;
            MonitorClicked();
        }

        void SubscribeTutorial()
        {
            if (_tutorialWindow == null) return;
            _tutorialWindow.StartRequested -= StartTutorial;
            _tutorialWindow.SkipRequested -= SkipTutorial;
            _tutorialWindow.StartRequested += StartTutorial;
            _tutorialWindow.SkipRequested += SkipTutorial;
        }

        void UnsubscribeTutorial()
        {
            if (_tutorialWindow == null) return;
            _tutorialWindow.StartRequested -= StartTutorial;
            _tutorialWindow.SkipRequested -= SkipTutorial;
        }

        void Start()
        {
            if (!Application.isPlaying || !enabled) return;
            if (EventSystem.current == null || !EventSystem.current.isActiveAndEnabled)
            {
                Debug.LogError("MainMenu requires an active authored EventSystem and input module. Configure and save them in Edit Mode.", this);
                enabled = false;
            }
        }

        public void MonitorClicked()
        {
            if (MainMenuTitleOverlay.IsBlocking) return;
            if (!enabled || Phase != MenuPhase.MainMenu3D || _accepted) return;
            Phase = MenuPhase.Zooming;
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            yield return cameraController.MoveToAuthoredPose(zoomTarget, introMoveDuration);
            if (!_callbacksAllowed || !isActiveAndEnabled) yield break;
            Phase = MenuPhase.Booting;
            cameraController.SetLookEnabled(true);
            bootOverlay.SetActive(true);
            float elapsed = 0f;
            float duration = Mathf.Max(0f, bootDuration);
            SetBootProgress(0f);
            while (elapsed < duration)
            {
                yield return null;
                if (!_callbacksAllowed || !isActiveAndEnabled) yield break;
                elapsed += Time.unscaledDeltaTime;
                SetBootProgress(Mathf.Clamp01(elapsed / duration));
            }
            SetBootProgress(1f);
            bootOverlay.SetActive(false);
            _introCompleted = true;
            Ready();
        }

        void Ready()
        {
            Phase = MenuPhase.DesktopReady;
            cameraController.SetLookEnabled(true);
            _generatedDesktopRoot.gameObject.SetActive(true);
            _desktop.RefreshAuthoredContent();
            if (!_tutorialForced) return;
            _tutorialForced = false;
            StartTutorial();
        }

        public void Launch(StageId ignored)
        {
            RequestPlay();
        }

        public void Play() { RequestPlay(); }

        public void RequestPlay()
        {
            if (!CanRequestLaunch()) return;
            if (!TutorialProgress.Seen)
            {
                if (_tutorialWindow == null || !_tutorialWindow.HasAuthoredContent)
                {
                    RecoverBeforeLoad("Tutorial prompt requires authored Start/Skip controls.");
                    return;
                }
                _desktop.ShowTutorialPrompt(true);
                _tutorialPromptOpen = true;
                return;
            }
            var run = CampaignRunStore.Load();
            if (run.IsComplete)
            {
                try
                {
                    _accepted = true;
                    Phase = MenuPhase.Launching;
                    _desktop.Lock();
                    SceneManager.LoadScene(endingSceneName, LoadSceneMode.Single);
                }
                catch (Exception exception) { RecoverBeforeLoad(exception.Message); }
                return;
            }
            StageSelection.Set(run.NextStage);
            BeginLaunch(interviewSceneName);
        }

        bool CanRequestLaunch() => !MainMenuTitleOverlay.IsBlocking && isActiveAndEnabled &&
            _callbacksAllowed && !_accepted && Phase == MenuPhase.DesktopReady;

        public void StartTutorial()
        {
            if (!CanRequestLaunch()) return;
            try { TutorialSceneFlow.Start(BeginLaunch); }
            catch (Exception exception) { RecoverBeforeLoad(exception.Message); }
        }

        public void SkipTutorial()
        {
            if (!CanRequestLaunch() || !_tutorialPromptOpen) return;
            TutorialProgress.MarkSeen();
            _tutorialPromptOpen = false;
            _tutorialWindow.Close();
            _desktop.ResetConnection();
            _desktop.ClearSelection();
        }

        public void StartNewRun()
        {
            if (!isActiveAndEnabled || !_callbacksAllowed || _accepted || Phase != MenuPhase.DesktopReady) return;
            CampaignRunStore.StartNewRun();
            _desktop.RefreshAuthoredContent();
        }

        void BeginLaunch(string sceneName)
        {
            if (!CanRequestLaunch()) return;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                RecoverBeforeLoad("Scene name is missing.");
                return;
            }
            _launchSceneName = sceneName;
            _accepted = true;
            Phase = MenuPhase.Launching;
            _desktop.Lock();
            try
            {
                if (onPlay != null) onPlay.Invoke(); else OpenAfterCenter();
            }
            catch (Exception exception) { RecoverBeforeLoad(exception.Message); }
        }
        void AlignCenter()
        {
            if (!transition.isActiveAndEnabled || MenuCamera() != Camera.main ||
                !transition.TryComputeCenteredPose(MenuCamera(), out _, out _, out _) ||
                !transition.TryComputeAlignedPose(MenuCamera(), out _, out _, out _))
            {
                RecoverBeforeLoad("MainMenu launch requires an enabled transition, the menu camera tagged MainCamera and a valid authored display frame/fill pose.");
                return;
            }
            transition.AlignToCenter(OpenAfterCenter);
        }
        void OpenAfterCenter()
        {
            if (!_callbacksAllowed || !isActiveAndEnabled || !_accepted ||
                Phase != MenuPhase.Launching || _alignmentCompleted) return;
            _alignmentCompleted = true;
            cameraController.SetLookEnabled(false);
            if (EventSystem.current == null || !EventSystem.current.isActiveAndEnabled ||
                transition == null || loadingPresenter == null || string.IsNullOrWhiteSpace(_launchSceneName))
            {
                RecoverBeforeLoad("MainMenu launch requires an active authored EventSystem, transition, loading presenter and scene name.");
                return;
            }
            try
            {
                if (!_launchFlow.TryStart(_launchSceneName, out var error))
                {
                    RecoverBeforeLoad("MainMenu scene launch failed: " + error);
                    return;
                }
            }
            catch (Exception exception) { RecoverBeforeLoad(exception.Message); return; }
            _tutorialPromptOpen = false;
            Phase = MenuPhase.Loading;
            loadingPresenter.Show();
            loadingPresenter.Render(_launchFlow.Snapshot);
        }

        void Update()
        {
            bool titleBlocking = MainMenuTitleOverlay.IsBlocking;
            if (_titleWasBlocking != titleBlocking)
            {
                _titleWasBlocking = titleBlocking;
                ApplyMonitorPicking();
                if (cameraController != null)
                    cameraController.SetLookEnabled(!titleBlocking &&
                        (Phase == MenuPhase.MainMenu3D || Phase == MenuPhase.DesktopReady || Phase == MenuPhase.Booting));
            }
            if (_tutorialPromptOpen && _tutorialWindow != null && _tutorialWindow.Root != null &&
                !_tutorialWindow.Root.gameObject.activeInHierarchy)
            {
                _tutorialPromptOpen = false;
                _desktop.ResetConnection();
                _desktop.ClearSelection();
            }
            if (!_callbacksAllowed || _launchFlow == null ||
                (Phase != MenuPhase.Loading && Phase != MenuPhase.FillZoom)) return;
            _launchFlow.Tick(Time.unscaledDeltaTime);
            loadingPresenter.Render(_launchFlow.Snapshot);
            if (Phase != MenuPhase.Loading || _zoomStarted || !_launchFlow.CanBeginZoom) return;
            _zoomStarted = true;
            Phase = MenuPhase.FillZoom;
            transition.Begin(ActivateInterview);
        }

        void RecoverBeforeLoad(string error)
        {
            // Once an async load is accepted its target and lock must remain intact.
            if (Phase == MenuPhase.Loading || Phase == MenuPhase.FillZoom || Phase == MenuPhase.Activating)
            {
                Debug.LogError(error, this);
                return;
            }
            _accepted = false;
            _alignmentCompleted = _zoomStarted = _activationRequested = false;
            _launchSceneName = null;
            _launchFlow = new SceneLaunchFlow(minimumLoadingDuration, loadingProgressSpeed, loadingCompleteHold);
            _desktop.Unlock();
            _desktop.ResetConnection();
            _desktop.ClearSelection();
            loadingPresenter.Hide();
            cameraController.SetAuthoredPose(zoomTarget);
            Ready();
            Debug.LogError(error, this);
        }

        void ActivateInterview()
        {
            if (!_callbacksAllowed || !isActiveAndEnabled || Phase != MenuPhase.FillZoom || _activationRequested) return;
            _activationRequested = true;
            Phase = MenuPhase.Activating;
            if (!_launchFlow.TryActivate())
                Debug.LogError("MainMenu scene activation was rejected; launch remains locked.", this);
        }

        void SetBootProgress(float progress)
        {
            bootProgressImage.fillAmount = progress;
            bootProgressText.text = $"시작하는 중… {Mathf.RoundToInt(progress * 100f)}%";
        }
        Camera MenuCamera() => cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;

#if UNITY_EDITOR
        public void BakeAuthoredUi()
        {
            if (Application.isPlaying) throw new InvalidOperationException("BakeAuthoredUi requires Edit Mode.");
            if (cameraController == null) cameraController = FindFirstObjectByType<MainMenuCameraController>();
            var screen = viewRoot != null ? viewRoot.GetComponentInChildren<ComputerScreen>(true) : FindFirstObjectByType<ComputerScreen>();
            if (screen == null || cameraController == null) throw new InvalidOperationException("Bake requires ComputerScreen and MainMenuCameraController.");
            viewRoot = screen.ViewRoot;
            UiKit.EnsureEventSystem();
            var camera = MenuCamera();
            if (camera.GetComponent<PhysicsRaycaster>() == null) camera.gameObject.AddComponent<PhysicsRaycaster>();
            monitorPhysicsRaycaster = camera.GetComponent<PhysicsRaycaster>();
            if (screen.GetComponent<Collider>() == null) screen.gameObject.AddComponent<BoxCollider>();
            monitorClick = screen.GetComponent<MonitorClickHandler>();
            if (monitorClick == null) monitorClick = screen.gameObject.AddComponent<MonitorClickHandler>();
            monitorClick.Bind(this);
            Canvas canvas = screen.GetComponentInChildren<Canvas>(true);
            if (canvas == null) canvas = new GameObject("MenuScreenCanvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            if (_desktop == null) screen.PlaceWorldCanvas(canvas.GetComponent<RectTransform>(), camera);
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (_desktop == null)
            {
                _generatedDesktopRoot = new GameObject("AuthoredDesktopRoot", typeof(RectTransform)).GetComponent<RectTransform>();
                _generatedDesktopRoot.SetParent(canvas.transform, false);
                UiKit.Stretch(_generatedDesktopRoot, 0f);
                _desktop = _generatedDesktopRoot.gameObject.AddComponent<DesktopView>();
                desktopLayout.IconTile = 144f;
                desktopLayout.IconCell = new Vector2(176f, 194f);
                desktopLayout.IconSpacing = 208f;
                _desktop.Build(_generatedDesktopRoot, desktopLayout, connectDelay, null, initialWindows);
            }
            _desktop.CaptureEditorReferences();
            if (_desktop.Tutorial != null) _desktop.Tutorial.BakePromptEditor();
            UnityEditor.EditorUtility.SetDirty(screen);
            UnityEditor.EditorUtility.SetDirty(camera);
            if (EventSystem.current != null) UnityEditor.EditorUtility.SetDirty(EventSystem.current.gameObject);
            if (bootOverlay == null)
            {
                Image boot = UiKit.Image(canvas.transform, "BootOverlay", new Color32(0xEC, 0x80, 0x32, 0xFF));
                UiKit.Stretch(boot.rectTransform, 0f);
                Text label = UiKit.Text(boot.transform, "BootLabel", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.text = "청강 OS\n시작하는 중…";
                UiKit.Stretch(label.rectTransform, 0f);
                bootOverlay = boot.gameObject;
            }
            if (bootProgressImage == null)
            {
                Image track = UiKit.Image(bootOverlay.transform, "BootProgressTrack", new Color32(0x42, 0x28, 0x18, 0xFF));
                track.rectTransform.anchorMin = new Vector2(0.2f, 0.25f);
                track.rectTransform.anchorMax = new Vector2(0.8f, 0.29f);
                track.rectTransform.offsetMin = track.rectTransform.offsetMax = Vector2.zero;
                bootProgressImage = UiKit.Image(track.transform, "BootProgress", Color.white);
                UiKit.Stretch(bootProgressImage.rectTransform, 0f);
                // Filled Image requires a sprite; reuse Unity's built-in white UI sprite.
                bootProgressImage.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                bootProgressImage.type = Image.Type.Filled;
                bootProgressImage.fillMethod = Image.FillMethod.Horizontal;
                bootProgressImage.fillOrigin = 0;
                bootProgressImage.fillAmount = 0f;
                track.raycastTarget = bootProgressImage.raycastTarget = false;
            }
            if (bootProgressText == null)
            {
                bootProgressText = UiKit.Text(bootOverlay.transform, "BootProgressText", 24, FontStyle.Normal, TextAnchor.MiddleCenter);
                bootProgressText.rectTransform.anchorMin = new Vector2(0.2f, 0.15f);
                bootProgressText.rectTransform.anchorMax = new Vector2(0.8f, 0.24f);
                bootProgressText.rectTransform.offsetMin = bootProgressText.rectTransform.offsetMax = Vector2.zero;
                bootProgressText.text = "시작하는 중… 0%";
                bootProgressText.raycastTarget = false;
            }
            if (transition == null) transition = FindFirstObjectByType<ComputerTransitionController>();
            if (loadingPresenter == null) loadingPresenter = FindFirstObjectByType<LoadingUiPresenter>();
            if (introStart == null) introStart = CreatePose("IntroStart", camera.transform.position, camera.transform.rotation);
            if (zoomTarget == null)
            {
                Vector3 center = screen.transform.position;
                Vector3 direction = (camera.transform.position - center).normalized;
                if (direction.sqrMagnitude < 0.01f) direction = screen.transform.forward;
                float distance = Mathf.Max(screen.transform.lossyScale.y, screen.transform.lossyScale.x / camera.aspect) * 0.55f /
                    Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
                Vector3 position = center + direction * Mathf.Max(0.1f, distance);
                zoomTarget = CreatePose("DesktopZoomTarget", position, Quaternion.LookRotation(center - position, camera.transform.up));
            }
            bootOverlay.SetActive(false);
            _generatedDesktopRoot.gameObject.SetActive(false);
            foreach (var component in canvas.GetComponentsInChildren<Component>(true))
                if (component != null) UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(monitorClick);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        Transform CreatePose(string name, Vector3 position, Quaternion rotation)
        {
            var pose = new GameObject(name).transform;
            pose.SetParent(transform, false);
            pose.SetPositionAndRotation(position, rotation);
            return pose;
        }
#endif
    }
}
