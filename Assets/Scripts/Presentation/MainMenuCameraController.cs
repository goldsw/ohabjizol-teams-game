using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ohabjizol.Presentation
{
    public sealed class MainMenuCameraController : MonoBehaviour
    {
        [SerializeField] Transform cameraPoseRoot;
        [SerializeField] Transform mouseLookPivot;
        [SerializeField] RectTransform computerScreenFrame;
        [SerializeField] float horizontalLookAmount = 0.6f;
        [SerializeField] float verticalLookAmount = 0.35f;
        [SerializeField] float smoothSpeed = 7f;
        [SerializeField] float maxYaw = 0.8f;
        [SerializeField] float maxPitch = 0.45f;
        [SerializeField] float maxHorizontalOffset = 0.22f;
        [SerializeField] float maxVerticalOffset = 0.12f;
        [SerializeField] float centerAlignDuration = 0.5f;

        Camera _camera;
        Vector3 _basePosition, _fromPosition, _toPosition;
        Quaternion _baseRotation, _fromRotation, _toRotation;
        Vector3 _pivotPosition;
        Quaternion _pivotRotation;
        Vector4 _offset, _alignFrom;
        float _fov, _fromFov, _toFov, _elapsed, _duration;
        bool _initialized, _locked, _moving, _aligning;
        int _version;
        AnimationCurve _curve;
        Action _done;

        public bool IsMoving => _moving || _aligning;
        public int MotionVersion => _version;
        public Camera ControlledCamera { get { Initialize(); return _camera; } }
        public void SetScreenFrame(RectTransform frame) { computerScreenFrame = frame; }
        bool HasRig => cameraPoseRoot != null && mouseLookPivot != null &&
            mouseLookPivot != cameraPoseRoot && mouseLookPivot.IsChildOf(cameraPoseRoot) &&
            _camera != null && _camera.transform != mouseLookPivot && _camera.transform.IsChildOf(mouseLookPivot);

        void Awake() { Initialize(); }
        void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = GetComponentInChildren<Camera>();
            Transform view = _camera != null ? _camera.transform : transform;
            _basePosition = view.position;
            _baseRotation = view.rotation;
            _fov = _camera != null ? _camera.fieldOfView : 60f;
            if (HasRig) { _pivotPosition = mouseLookPivot.localPosition; _pivotRotation = mouseLookPivot.localRotation; }
        }

        void CancelMotion()
        {
            ++_version;
            _moving = _aligning = false;
            _done = null;
        }
        public void CancelMove() { CancelMotion(); }
        void OnDisable() { CancelMotion(); }
        public void LockLook() { SetLookEnabled(false); }
        public void SetLookEnabled(bool enabled)
        {
            Initialize();
            if (enabled && _aligning) CancelMotion();
            _locked = !enabled;
        }
        public void SetAuthoredPose(Transform pose)
        {
            if (pose == null) throw new ArgumentNullException(nameof(pose));
            Initialize();
            SetPose(pose.position, pose.rotation, _fov);
        }
        public void SetPose(Vector3 position, Quaternion rotation, float fov)
        {
            Initialize();
            CancelMotion();
            _locked = true;
            _offset = Vector4.zero;
            _basePosition = position;
            _baseRotation = rotation;
            _fov = Mathf.Clamp(fov, 1f, 179f);
            ApplyPose();
        }
        public IEnumerator MoveToAuthoredPose(Transform pose, float duration)
        {
            if (pose == null) throw new ArgumentNullException(nameof(pose));
            Initialize();
            MoveToPose(pose.position, pose.rotation, _fov, duration, null, null);
            int version = _version;
            while (version == _version && IsMoving) yield return null;
        }
        public void MoveToPose(Vector3 position, Quaternion rotation, float fov, float duration,
            AnimationCurve curve, Action done)
        {
            Initialize();
            CancelMotion();
            _fromPosition = _basePosition; _fromRotation = _baseRotation; _fromFov = _fov;
            _toPosition = position; _toRotation = rotation; _toFov = Mathf.Clamp(fov, 1f, 179f);
            _elapsed = 0f; _duration = Mathf.Max(0f, duration); _curve = curve; _done = done;
            _moving = true;
        }
        public void AlignToCenter(Action done) { AlignToFrame(computerScreenFrame, done); }
        public void AlignToFrame(RectTransform frame, Action done)
        {
            Initialize();
            CancelMotion();
            _locked = true;
            _alignFrom = _offset;
            _fromPosition = _basePosition; _fromRotation = _baseRotation;
            _toPosition = _basePosition; _toRotation = _baseRotation;
            if (frame != null && TryComputeCenteredPose(frame, _basePosition, out var position, out var rotation))
            { _toPosition = position; _toRotation = rotation; }
            _duration = Mathf.Max(0f, centerAlignDuration); _elapsed = 0f; _done = done; _aligning = true;
        }
        public static bool TryComputeCenteredPose(RectTransform frame, Vector3 from,
            out Vector3 position, out Quaternion rotation)
        {
            position = from; rotation = Quaternion.identity;
            if (!TryGetFrame(frame, out var center, out var normal, out var up, out _, out _)) return false;
            if (Vector3.Dot(normal, from - center) < 0f) normal = -normal;
            position = center + normal * Mathf.Max(0.05f, Vector3.Distance(from, center));
            rotation = Quaternion.LookRotation(-normal, up);
            return true;
        }
        public static bool TryGetFrame(RectTransform frame, out Vector3 center, out Vector3 normal,
            out Vector3 up, out float width, out float height)
        {
            center = normal = up = Vector3.zero; width = height = 0f;
            if (frame == null) return false;
            var corners = new Vector3[4];
            frame.GetWorldCorners(corners);
            Vector3 right = corners[3] - corners[0];
            Vector3 vertical = corners[1] - corners[0];
            width = right.magnitude; height = vertical.magnitude;
            if (width < 0.0001f || height < 0.0001f) return false;
            normal = Vector3.Cross(right, vertical).normalized;
            if (normal.sqrMagnitude < 0.5f || Mathf.Abs(Vector3.Dot(right.normalized, vertical.normalized)) > 0.001f) return false;
            up = vertical / height;
            center = (corners[0] + corners[2]) * 0.5f;
            return true;
        }
        void LateUpdate()
        {
            Initialize();
            Action completed = null;
            if (_moving || _aligning)
            {
                _elapsed += Time.unscaledDeltaTime;
                float t = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
                float blend = t >= 1f ? 1f : (_curve != null && _moving ? Mathf.Clamp01(_curve.Evaluate(t)) : Mathf.SmoothStep(0f, 1f, t));
                _basePosition = Vector3.Lerp(_fromPosition, _toPosition, blend);
                _baseRotation = Quaternion.Slerp(_fromRotation, _toRotation, blend);
                if (_moving) _fov = Mathf.Lerp(_fromFov, _toFov, blend);
                if (_aligning) _offset = Vector4.Lerp(_alignFrom, Vector4.zero, blend);
                if (t >= 1f) { _moving = _aligning = false; completed = _done; _done = null; }
            }
            if (!_locked && !_aligning)
            {
                Vector2 mouse = Vector2.zero;
                if (Mouse.current != null && Screen.width > 0 && Screen.height > 0)
                {
                    Vector2 p = Mouse.current.position.ReadValue();
                    mouse = new Vector2(Mathf.Clamp(p.x / Screen.width * 2f - 1f, -1f, 1f), Mathf.Clamp(p.y / Screen.height * 2f - 1f, -1f, 1f));
                }
                var target = new Vector4(Mathf.Clamp(mouse.x * horizontalLookAmount, -Mathf.Abs(maxYaw), Mathf.Abs(maxYaw)),
                    Mathf.Clamp(-mouse.y * verticalLookAmount, -Mathf.Abs(maxPitch), Mathf.Abs(maxPitch)),
                    mouse.x * maxHorizontalOffset, mouse.y * maxVerticalOffset);
                _offset = Vector4.Lerp(_offset, target, smoothSpeed <= 0f ? 1f : 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime));
            }
            ApplyPose();
            completed?.Invoke();
        }
        void ApplyPose()
        {
            Transform view = _camera != null ? _camera.transform : transform;
            Quaternion drift = Quaternion.Euler(_offset.y, _offset.x, 0f);
            Vector3 slide = new Vector3(_offset.z, _offset.w, 0f);
            if (HasRig)
            {
                mouseLookPivot.SetLocalPositionAndRotation(_pivotPosition, _pivotRotation);
                Quaternion delta = _baseRotation * Quaternion.Inverse(view.rotation);
                cameraPoseRoot.SetPositionAndRotation(_basePosition + delta * (cameraPoseRoot.position - view.position), delta * cameraPoseRoot.rotation);
                mouseLookPivot.SetLocalPositionAndRotation(_pivotPosition + slide, _pivotRotation * drift);
            }
            else view.SetPositionAndRotation(_basePosition + _baseRotation * slide, _baseRotation * drift);
            if (_camera != null) _camera.fieldOfView = _fov;
        }
    }
}
