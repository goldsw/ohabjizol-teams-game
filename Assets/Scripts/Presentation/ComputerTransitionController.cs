using System;
using UnityEngine;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Moves the menu camera onto the monitor after loading is ready.
    /// Position does the approach. Field of view only helps the quad fill the view.
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
        Action _onComplete;

        public bool IsRunning
        {
            get { return _running; }
        }

        void Awake()
        {
            if (transitionCurve == null || transitionCurve.length == 0)
                transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
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

            var look = _camera.GetComponent<MainMenuCameraController>();
            if (look != null)
                look.LockLook();

            _startPosition = _camera.transform.position;
            _startRotation = _camera.transform.rotation;
            _startFov = _camera.fieldOfView;
            _endFov = targetFov > 1f ? targetFov : _startFov;
            ComputeEndPose();
            _elapsed = 0f;
            _running = true;
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
                Finish();
        }

        void ResolveScreen()
        {
            if (targetComputerScreen != null)
                return;

            var screen = FindFirstObjectByType<ComputerScreen>();
            if (screen != null)
                targetComputerScreen = screen.transform;
        }

        void ComputeEndPose()
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

            float verticalFov = _endFov * Mathf.Deg2Rad;
            float distanceForHeight = (height * 0.5f) / Mathf.Tan(verticalFov * 0.5f);
            float aspect = Mathf.Max(0.01f, _camera.aspect);
            float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * aspect);
            float distanceForWidth = (width * 0.5f) / Mathf.Tan(horizontalFov * 0.5f);
            float distance = Mathf.Max(distanceForHeight, distanceForWidth) * 0.96f;

            Vector3 toCamera = _startPosition - screen.position;
            Vector3 approachAxis = screen.forward;
            if (Vector3.Dot(approachAxis, toCamera) < 0f)
                approachAxis = -approachAxis;

            _endPosition = screen.position + approachAxis * distance + screen.TransformVector(targetPositionOffset);
            Vector3 lookDirection = screen.position - _endPosition;
            if (lookDirection.sqrMagnitude < 0.0001f)
                lookDirection = -approachAxis;
            _endRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        void Finish()
        {
            _running = false;
            var done = _onComplete;
            _onComplete = null;
            if (done != null)
                done();
        }
    }
}
