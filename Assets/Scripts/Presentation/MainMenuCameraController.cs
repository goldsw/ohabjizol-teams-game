using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Main-menu camera drift. Mouse position slides the camera; rotation is only a small tilt.
    /// Screen position is mapped to about -1..1 from the center, then clamped.
    /// Tuned distances live on the inspector fields, not in the formula.
    /// </summary>
    public sealed class MainMenuCameraController : MonoBehaviour
    {
        [SerializeField] float horizontalLookAmount = 0.6f;
        [SerializeField] float verticalLookAmount = 0.35f;
        [SerializeField] float smoothSpeed = 7f;
        [SerializeField] float maxYaw = 0.8f;
        [SerializeField] float maxPitch = 0.45f;
        [SerializeField] float maxHorizontalOffset = 0.22f;
        [SerializeField] float maxVerticalOffset = 0.12f;
        [SerializeField] float centerAlignDuration = 0.5f;

        Vector3 _initialLocalPosition;
        Quaternion _initialLocalRotation;
        float _yaw;
        float _pitch;
        float _offsetX;
        float _offsetY;
        float _yawVelocity;
        float _pitchVelocity;
        float _offsetXVelocity;
        float _offsetYVelocity;
        bool _locked;
        bool _aligning;
        float _alignElapsed;
        float _alignFromYaw;
        float _alignFromPitch;
        float _alignFromOffsetX;
        float _alignFromOffsetY;
        Action _onAligned;

        void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
        }

        public void LockLook()
        {
            _locked = true;
        }

        /// <summary>
        /// Lerps the current mouse drift back to the center pose. Offsets stay intact
        /// until the move finishes, then they are cleared and look is locked.
        /// </summary>
        public void AlignToCenter(Action onComplete)
        {
            if (_aligning)
                return;

            _onAligned = onComplete;
            _alignFromYaw = _yaw;
            _alignFromPitch = _pitch;
            _alignFromOffsetX = _offsetX;
            _alignFromOffsetY = _offsetY;
            _alignElapsed = 0f;
            _aligning = true;
        }

        void LateUpdate()
        {
            if (_aligning)
            {
                AdvanceCenterAlign();
                return;
            }

            if (_locked)
                return;

            float nx;
            float ny;
            ReadNormalizedMouse(out nx, out ny);

            float yawLimit = Mathf.Abs(maxYaw);
            float pitchLimit = Mathf.Abs(maxPitch);
            float xLimit = Mathf.Abs(maxHorizontalOffset);
            float yLimit = Mathf.Abs(maxVerticalOffset);

            float yawTarget = Mathf.Clamp(nx * horizontalLookAmount, -yawLimit, yawLimit);
            float pitchTarget = Mathf.Clamp(-ny * verticalLookAmount, -pitchLimit, pitchLimit);
            float offsetXTarget = Mathf.Clamp(nx * maxHorizontalOffset, -xLimit, xLimit);
            float offsetYTarget = Mathf.Clamp(ny * maxVerticalOffset, -yLimit, yLimit);

            if (smoothSpeed <= 0f)
            {
                _yaw = yawTarget;
                _pitch = pitchTarget;
                _offsetX = offsetXTarget;
                _offsetY = offsetYTarget;
            }
            else
            {
                float smoothTime = 1f / smoothSpeed;
                _yaw = Mathf.SmoothDamp(_yaw, yawTarget, ref _yawVelocity, smoothTime);
                _pitch = Mathf.SmoothDamp(_pitch, pitchTarget, ref _pitchVelocity, smoothTime);
                _offsetX = Mathf.SmoothDamp(_offsetX, offsetXTarget, ref _offsetXVelocity, smoothTime);
                _offsetY = Mathf.SmoothDamp(_offsetY, offsetYTarget, ref _offsetYVelocity, smoothTime);
            }

            ApplyDrift();
        }

        void AdvanceCenterAlign()
        {
            float duration = centerAlignDuration > 0f ? centerAlignDuration : 0.5f;
            _alignElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_alignElapsed / duration);
            _yaw = Mathf.Lerp(_alignFromYaw, 0f, t);
            _pitch = Mathf.Lerp(_alignFromPitch, 0f, t);
            _offsetX = Mathf.Lerp(_alignFromOffsetX, 0f, t);
            _offsetY = Mathf.Lerp(_alignFromOffsetY, 0f, t);
            ApplyDrift();

            if (t < 1f)
                return;

            _yaw = 0f;
            _pitch = 0f;
            _offsetX = 0f;
            _offsetY = 0f;
            _yawVelocity = 0f;
            _pitchVelocity = 0f;
            _offsetXVelocity = 0f;
            _offsetYVelocity = 0f;
            ApplyDrift();
            _aligning = false;
            LockLook();

            var done = _onAligned;
            _onAligned = null;
            if (done != null)
                done();
        }

        void ApplyDrift()
        {
            transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f) * _initialLocalRotation;
            transform.localPosition = _initialLocalPosition + new Vector3(_offsetX, _offsetY, 0f);
        }

        static void ReadNormalizedMouse(out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (Mouse.current == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Vector2 position = Mouse.current.position.ReadValue();
            x = Mathf.Clamp((position.x / Screen.width - 0.5f) * 2f, -1f, 1f);
            y = Mathf.Clamp((position.y / Screen.height - 0.5f) * 2f, -1f, 1f);
        }
    }
}
