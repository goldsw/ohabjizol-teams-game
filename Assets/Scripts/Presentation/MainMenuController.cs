using System;
using System.Collections;
using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] string loadingSceneName = "LoadingScene";
        [SerializeField] string endingSceneName = "EndingScene";
        [SerializeField] Transform viewRoot;
        [SerializeField] MainMenuCameraController cameraController;
        [SerializeField] float connectDelay = 1f;
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
        public Action onPlay;
        [SerializeField] PhysicsRaycaster monitorPhysicsRaycaster;
        bool _monitorPickingCaptured, _monitorOriginallyEnabled;
        MenuPhase _phase;
        bool _accepted;
        static bool _introCompleted;
        public enum MenuPhase { MainMenu3D, Zooming, Booting, DesktopReady, Launching }
        public MenuPhase Phase
        {
            get => _phase;
            private set { _phase = value; ApplyMonitorPicking(); }
        }
        public bool HasAuthoredUi => _desktop != null && _desktop.HasAuthoredUi &&
            _generatedDesktopRoot != null && bootOverlay != null && introStart != null &&
            zoomTarget != null && cameraController != null && monitorClick != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { _introCompleted = false; }

        void Awake()
        {
            if (!Application.isPlaying) return;
            if (!HasAuthoredUi)
            {
                Debug.LogError("MainMenu requires baked Desktop/window references, camera poses, monitor click handler and EventSystem. Run BakeAuthoredUi in Edit Mode and save.", this);
                enabled = false;
                return;
            }
            if (!_desktop.Bind(connectDelay, Launch)) { enabled = false; return; }
            monitorClick.Bind(this);
            onPlay = AlignCenter;
            bootOverlay.SetActive(false);
            _generatedDesktopRoot.gameObject.SetActive(false);
            if (_introCompleted)
            {
                cameraController.SetAuthoredPose(zoomTarget);
                Ready();
            }
            else
            {
                cameraController.SetAuthoredPose(introStart);
                Phase = MenuPhase.MainMenu3D;
            }
        }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
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
                monitorPhysicsRaycaster.enabled = _monitorOriginallyEnabled && enabled && Phase == MenuPhase.MainMenu3D;
        }

        void RestoreMonitorPicking()
        {
            if (_monitorPickingCaptured && monitorPhysicsRaycaster)
                monitorPhysicsRaycaster.enabled = _monitorOriginallyEnabled;
            _monitorPickingCaptured = false;
        }

        void OnDisable() { RestoreMonitorPicking(); }
        void OnDestroy() { RestoreMonitorPicking(); }

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
            if (!enabled || Phase != MenuPhase.MainMenu3D || _accepted) return;
            Phase = MenuPhase.Zooming;
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            yield return cameraController.MoveToAuthoredPose(zoomTarget, introMoveDuration);
            Phase = MenuPhase.Booting;
            bootOverlay.SetActive(true);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, bootDuration));
            bootOverlay.SetActive(false);
            _introCompleted = true;
            Ready();
        }

        void Ready()
        {
            Phase = MenuPhase.DesktopReady;
            _generatedDesktopRoot.gameObject.SetActive(true);
            _desktop.RefreshAuthoredContent();
        }

        public void Launch(StageId ignored)
        {
            if (_accepted || Phase != MenuPhase.DesktopReady) return;
            var run = CampaignRunStore.Load();
            if (run.IsComplete)
            {
                _accepted = true;
                Phase = MenuPhase.Launching;
                _desktop.Lock();
                SceneManager.LoadScene(endingSceneName, LoadSceneMode.Single);
                return;
            }
            StageSelection.Set(run.NextStage);
            Play();
        }

        public void StartNewRun()
        {
            if (_accepted || Phase != MenuPhase.DesktopReady) return;
            CampaignRunStore.StartNewRun();
            _desktop.RefreshAuthoredContent();
        }

        public void Play()
        {
            if (_accepted || Phase != MenuPhase.DesktopReady) return;
            _accepted = true;
            Phase = MenuPhase.Launching;
            _desktop.Lock();
            if (onPlay != null) onPlay.Invoke(); else OpenAfterCenter();
        }
        void AlignCenter() { cameraController.AlignToCenter(OpenAfterCenter); }
        void OpenAfterCenter()
        {
            ComputerTransitionController.RememberPose(MenuCamera());
            SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
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
