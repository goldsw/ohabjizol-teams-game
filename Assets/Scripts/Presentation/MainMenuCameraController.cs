using UnityEngine;
using UnityEngine.InputSystem;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Limited mouse look for the main-menu camera.
    /// Screen position is mapped to about -1..1 from the center, then clamped.
    /// Tuned distances live on the inspector fields, not in the formula.
    /// </summary>
    public sealed class MainMenuCameraController : MonoBehaviour
    {
        [SerializeField] float horizontalLookAmount = 5f;
        [SerializeField] float verticalLookAmount = 3.5f;
        [SerializeField] float smoothSpeed = 7f;
        [SerializeField] float maxYaw = 7f;
        [SerializeField] float maxPitch = 5f;
        [SerializeField] float maxHorizontalOffset = 0.08f;
        [SerializeField] float maxVerticalOffset = 0.05f;

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

        void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
        }

        public void LockLook()
        {
            _locked = true;
        }

        void LateUpdate()
        {
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
