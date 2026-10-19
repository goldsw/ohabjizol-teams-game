using System;
using UnityEngine;

namespace Ohabjizol.Presentation
{
    /// <summary>Computes screen framing; MainMenuCameraController owns all camera writes.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class ComputerTransitionController : MonoBehaviour
    {
        [SerializeField] Transform targetComputerScreen;
        [SerializeField] RectTransform screenFrame;
        [SerializeField] float transitionDuration = 1.6f;
        [SerializeField] Vector3 targetPositionOffset = Vector3.zero;
        [SerializeField] float targetFov = 42f;
        [SerializeField] AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        MainMenuCameraController _owner;
        Camera _camera;
        bool _running;
        int _request, _motionVersion;
        Action _onComplete;
        static bool _hasFixedPose;
        static Vector3 _fixedPosition;
        static Quaternion _fixedRotation;
        static float _fixedFov;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetFixedPose() { _hasFixedPose = false; }

        // Legacy loading cameras are upgraded to the same single writer before any pose write.
        static MainMenuCameraController EnsureOwner(Camera camera)
        {
            var owner = camera.GetComponent<MainMenuCameraController>();
            if (owner == null) owner = camera.GetComponentInParent<MainMenuCameraController>();
            if (owner != null && owner.ControlledCamera != camera) owner = null;
            if (owner == null) owner = camera.gameObject.AddComponent<MainMenuCameraController>();
            return owner;
        }
        public static void RememberPose(Camera camera)
        {
            if (camera == null) return;
            _fixedPosition = camera.transform.position;
            _fixedRotation = camera.transform.rotation;
            _fixedFov = camera.fieldOfView;
            _hasFixedPose = true;
        }
        public static void ApplyFixedPose(Camera camera)
        {
            if (!_hasFixedPose || camera == null) return;
            EnsureOwner(camera).SetPose(_fixedPosition, _fixedRotation, _fixedFov);
        }
        public bool IsRunning => _running && _owner != null && _owner.isActiveAndEnabled &&
            _owner.MotionVersion == _motionVersion && _owner.IsMoving;
        public RectTransform ScreenFrame => screenFrame;
        public void SetScreenFrame(RectTransform frame) { screenFrame = frame; }

        void Awake()
        {
            if (transitionCurve == null || transitionCurve.length == 0)
                transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            var menu = GetComponent<MainMenuController>();
            if (menu != null && menu.onPlay == null) menu.onPlay = CenterAndLock;
        }
        void OnDisable()
        {
            ++_request;
            _onComplete = null;
            _running = false;
            if (_owner != null && _owner.MotionVersion == _motionVersion) _owner.CancelMove();
        }
        RectTransform ResolveFrame()
        {
            if (screenFrame != null) return screenFrame;
            // Only an explicitly supplied RectTransform is accepted as a legacy frame.
            // Renderer bounds and arbitrary descendant Canvas bounds are not display geometry.
            if (targetComputerScreen != null) return targetComputerScreen as RectTransform;
            return null;
        }
        public bool TryComputeCenteredPose(Camera camera, out Vector3 position, out Quaternion rotation, out float fieldOfView)
        {
            position = camera != null ? camera.transform.position : Vector3.zero;
            rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
            fieldOfView = camera != null ? camera.fieldOfView : targetFov;
            return camera != null && MainMenuCameraController.TryComputeCenteredPose(ResolveFrame(), position, out position, out rotation);
        }
        public void CenterAndLock()
        {
            var camera = Camera.main;
            if (!TryComputeCenteredPose(camera, out var position, out var rotation, out var fov))
            {
                Debug.LogError("Computer centering needs a main camera and an explicit display RectTransform frame.", this);
                return;
            }
            EnsureOwner(camera).SetPose(position, rotation, fov);
        }
        /// <summary>Blends drift to zero and centers on the explicit frame before invoking done.</summary>
        public void AlignToCenter(Action done)
        {
            if (!isActiveAndEnabled) return;
            var camera = Camera.main;
            var frame = ResolveFrame();
            if (camera == null || !MainMenuCameraController.TryGetFrame(frame, out _, out _, out _, out _, out _))
            {
                Debug.LogError("Computer alignment needs a main camera and an explicit display RectTransform frame.", this);
                return;
            }
            StartRequest(camera, done);
            int request = _request;
            _owner.AlignToFrame(frame, () => Finish(request));
            _motionVersion = _owner.MotionVersion;
        }
        void StartRequest(Camera camera, Action done)
        {
            ++_request;
            if (_owner != null && _owner.MotionVersion == _motionVersion) _owner.CancelMove();
            _camera = camera;
            _owner = EnsureOwner(camera);
            _onComplete = done;
            _running = true;
        }
        public void Begin(Action onComplete)
        {
            if (!isActiveAndEnabled || IsRunning) return;
            var camera = Camera.main;
            if (!TryComputeAlignedPose(camera, out var position, out var rotation, out var fov))
            {
                Debug.LogError("Computer fill needs a perspective main camera and a valid rectangular display frame.", this);
                return;
            }
            StartRequest(camera, onComplete);
            int request = _request;
            _owner.LockLook();
            _owner.MoveToPose(position, rotation, fov, transitionDuration, transitionCurve, () => Finish(request));
            _motionVersion = _owner.MotionVersion;
        }
        /// <summary>Legacy name retained: computes the final cover/fill pose, not the pre-load centered pose.</summary>
        public bool TryComputeAlignedPose(Camera camera, out Vector3 position, out Quaternion rotation, out float fieldOfView)
        {
            position = camera != null ? camera.transform.position : Vector3.zero;
            rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
            fieldOfView = camera != null ? camera.fieldOfView : targetFov;
            if (camera == null || camera.orthographic || camera.aspect <= 0f) return false;
            if (!MainMenuCameraController.TryGetFrame(ResolveFrame(), out var center, out var normal,
                out var up, out var width, out var height)) return false;
            if (Vector3.Dot(normal, position - center) < 0f) normal = -normal;
            rotation = Quaternion.LookRotation(-normal, up);
            fieldOfView = Mathf.Clamp(targetFov > 1f ? targetFov : camera.fieldOfView, 1f, 179f);
            float tangent = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
            // Cover: viewport must fit inside the display in both dimensions, hence MIN.
            // Account for authored lateral displacement before computing the allowed distance.
            float halfWidth = width * 0.5f - Mathf.Abs(targetPositionOffset.x);
            float halfHeight = height * 0.5f - Mathf.Abs(targetPositionOffset.y);
            if (halfWidth <= 0f || halfHeight <= 0f) return false;
            float distance = Mathf.Min(halfHeight / tangent, halfWidth / (tangent * camera.aspect)) * 0.96f;
            distance -= Mathf.Max(0f, targetPositionOffset.z);
            if (distance <= camera.nearClipPlane * 1.01f || float.IsNaN(distance) || float.IsInfinity(distance)) return false;
            position = center + normal * distance + rotation * new Vector3(targetPositionOffset.x, targetPositionOffset.y, 0f);
            return true;
        }
        void Finish(int request)
        {
            if (!isActiveAndEnabled || !_running || request != _request || _owner == null ||
                _owner.MotionVersion != _motionVersion) return;
            _running = false;
            RememberPose(_camera);
            var done = _onComplete;
            _onComplete = null;
            done?.Invoke();
        }
    }
}
