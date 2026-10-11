using System;
using UnityEngine;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Frames the monitor in two steps. <see cref="CenterAndLock"/> runs from the menu
    /// play callback and only centers, at the current distance and field of view.
    /// <see cref="Begin"/> runs after loading and only zooms in along that centered view.
    /// <see cref="RememberPose"/> carries the centered pose into the loading scene.
    /// Screen art can change as long as a <see cref="ComputerScreen"/> marks the display.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class ComputerTransitionController : MonoBehaviour
    {
        [SerializeField] Transform targetComputerScreen;
        [SerializeField] float transitionDuration = 1.6f;
        [SerializeField] Vector3 targetPositionOffset = Vector3.zero;
        [SerializeField] float targetFov = 42f;
        [SerializeField] AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        Camera _camera;
        Vector3 _startPosition;
        Quaternion _startRotation;
        float _startFov;
        float _endFov;
        Vector3 _endPosition;
        Quaternion _endRotation;
        float _elapsed;
        bool _running;
        bool _aligned;
        Action _onComplete;
        static bool _hasFixedPose;
        static Vector3 _fixedPosition;
        static Quaternion _fixedRotation;
        static float _fixedFov;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetFixedPose()
        {
            _hasFixedPose = false;
        }

        public static void RememberPose(Camera camera)
        {
            if (camera == null)
                return;

            _fixedPosition = camera.transform.position;
            _fixedRotation = camera.transform.rotation;
            _fixedFov = camera.fieldOfView;
            _hasFixedPose = true;
        }

        public static void ApplyFixedPose(Camera camera)
        {
            if (!_hasFixedPose || camera == null)
                return;

            camera.transform.SetPositionAndRotation(_fixedPosition, _fixedRotation);
            camera.fieldOfView = _fixedFov;
        }

        public bool IsRunning
        {
            get { return _running; }
        }

        void Awake()
        {
            if (transitionCurve == null || transitionCurve.length == 0)
                transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            var menu = GetComponent<MainMenuController>();
            if (menu != null && menu.onPlay == null)
                menu.onPlay = CenterAndLock;
        }

        /// <summary>
        /// Centers the main camera on the screen and locks look. Distance and field of
        /// view stay as they are, so this does not zoom.
        /// </summary>
        public void CenterAndLock()
        {
            ResolveScreen();
            var camera = Camera.main;
            if (camera == null || targetComputerScreen == null)
                return;

            var look = camera.GetComponent<MainMenuCameraController>();
            if (look != null)
                look.LockLook();

            Vector3 screenPosition = targetComputerScreen.position;
            Vector3 offset = camera.transform.position - screenPosition;
            float distance = offset.magnitude;
            if (distance < 0.05f)
                distance = 0.05f;

            Vector3 axis = targetComputerScreen.forward;
            if (axis.sqrMagnitude < 0.0001f || Vector3.Dot(axis, offset) < 0f)
                axis = -axis;
            if (axis.sqrMagnitude < 0.0001f)
                axis = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.back;

            camera.transform.position = screenPosition + axis.normalized * distance;
            Vector3 lookDirection = screenPosition - camera.transform.position;
            if (lookDirection.sqrMagnitude < 0.0001f)
                lookDirection = -axis;
            camera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        public void Begin(Action onComplete)
        {
            if (_running)
                return;

            _onComplete = onComplete;
            ResolveScreen();
            _camera = Camera.main;
            if (_camera == null || targetComputerScreen == null)
            {
                Debug.LogError("Computer transition needs a main camera and a computer screen.");
                Finish();
                return;
            }

            _startPosition = _camera.transform.position;
            _startRotation = _camera.transform.rotation;
            _startFov = _camera.fieldOfView;
            _endFov = targetFov > 1f ? targetFov : _startFov;
            ComputeZoomPose();
            _elapsed = 0f;
            _aligned = false;
            _running = true;
        }

        public bool TryComputeAlignedPose(Camera camera, out Vector3 position, out Quaternion rotation, out float fieldOfView)
        {
            position = camera != null ? camera.transform.position : Vector3.zero;
            rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
            fieldOfView = camera != null ? camera.fieldOfView : targetFov;
            if (camera == null)
                return false;

            ResolveScreen();
            if (targetComputerScreen == null)
                return false;

            _camera = camera;
            _startPosition = camera.transform.position;
            _startRotation = camera.transform.rotation;
            _endFov = targetFov > 1f ? targetFov : camera.fieldOfView;
            ComputeZoomPose();
            position = _endPosition;
            rotation = _endRotation;
            fieldOfView = _endFov;
            return true;
        }

        void LateUpdate()
        {
            if (!_running || _camera == null)
                return;

            _elapsed += Time.deltaTime;
            float normalized = transitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_elapsed / transitionDuration);
            float curved = transitionCurve != null && transitionCurve.length > 0
                ? transitionCurve.Evaluate(normalized)
                : normalized;
            curved = Mathf.Clamp01(curved);

            _camera.transform.SetPositionAndRotation(
                Vector3.Lerp(_startPosition, _endPosition, curved),
                Quaternion.Slerp(_startRotation, _endRotation, curved));

            float fovBlend = curved * curved;
            _camera.fieldOfView = Mathf.Lerp(_startFov, _endFov, fovBlend);

            if (normalized >= 1f)
            {
                _aligned = true;
                Finish();
            }
        }

        void ResolveScreen()
        {
            if (targetComputerScreen != null)
                return;

            var screen = FindFirstObjectByType<ComputerScreen>();
            if (screen != null)
                targetComputerScreen = screen.transform;
        }

        void ComputeZoomPose()
        {
            float distance = FillDistance(_endFov);
            Vector3 forward = _camera.transform.forward;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 toScreen = targetComputerScreen.position - _camera.transform.position;
            float along = Vector3.Dot(toScreen, forward);
            if (along < 0.05f)
                along = toScreen.magnitude;

            float travel = along - distance;
            _endPosition = _camera.transform.position + forward * travel + _camera.transform.TransformVector(targetPositionOffset);
            _endRotation = _camera.transform.rotation;
        }

        float FillDistance(float fovDegrees)
        {
            var screen = targetComputerScreen;
            float width = Mathf.Abs(screen.lossyScale.x);
            float height = Mathf.Abs(screen.lossyScale.y);
            var screenRenderer = screen.GetComponent<Renderer>();
            if (screenRenderer != null)
            {
                width = Mathf.Max(width, screenRenderer.bounds.size.x);
                height = Mathf.Max(height, screenRenderer.bounds.size.y);
            }

            width = Mathf.Max(width, 0.01f);
            height = Mathf.Max(height, 0.01f);
            float verticalFov = Mathf.Max(1f, fovDegrees) * Mathf.Deg2Rad;
            float distanceForHeight = (height * 0.5f) / Mathf.Tan(verticalFov * 0.5f);
            float aspect = _camera != null ? _camera.aspect : 16f / 9f;
            if (aspect < 0.2f)
                aspect = 16f / 9f;
            float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * aspect);
            float distanceForWidth = (width * 0.5f) / Mathf.Tan(horizontalFov * 0.5f);
            return Mathf.Max(distanceForHeight, distanceForWidth) * 0.96f;
        }

        void Finish()
        {
            if (_aligned && _camera != null)
            {
                _fixedPosition = _camera.transform.position;
                _fixedRotation = _camera.transform.rotation;
                _fixedFov = _camera.fieldOfView;
                _hasFixedPose = true;
            }

            _running = false;
            var done = _onComplete;
            _onComplete = null;
            if (done != null)
                done();
        }
    }
}
