using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [DisallowMultipleComponent]
    public sealed class DownloadItemView : MonoBehaviour
    {
        [SerializeField] DocumentKind _kind;
        [SerializeField] Button _button;
        [SerializeField] Image _fill;
        [SerializeField] Image _statusIcon;
        [SerializeField] Sprite _downloadSprite;
        [SerializeField] Sprite _checkSprite;
        [SerializeField] float _minimumDownloadSeconds = .2f;
        [SerializeField] float _maximumDownloadSeconds = 1f;

        DocumentDownloadController _controller;
        Button _boundButton;
        uint _completionSerial;
        bool _destroyed;

        public DocumentKind Kind => _kind;
        public Button Button => _button;
        public bool HasAuthoredUi => _button != null && _fill != null &&
            _statusIcon != null && _downloadSprite != null && _checkSprite != null;
        public event Action<DocumentDownloadRequest> Completed;

        void Awake() => Initialize();

        void Initialize()
        {
            if (_destroyed || _controller != null) return;
            _controller = new DocumentDownloadController(_kind);
            _controller.Completed += HandleCompleted;
            _boundButton = _button;
            if (_boundButton != null) _boundButton.onClick.AddListener(BeginDownload);
            Render();
        }

        public void BindContext(string key)
        {
            Initialize();
            if (_destroyed) return;
            _controller.BindContext(key);
            Render();
        }

        public void CancelPending()
        {
            Initialize();
            if (_destroyed) return;
            _controller.CancelPending();
            Render();
        }

        public bool IsCurrent(DocumentDownloadRequest request)
        {
            Initialize();
            return !_destroyed && _controller.IsCurrent(request);
        }

        public void TickDownloads(float deltaTime)
        {
            Initialize();
            if (_destroyed) return;
            uint serial = _completionSerial;
            _controller.Tick(deltaTime);
            // Subscribers can replace context, tick recursively or destroy this item.
            if (!_destroyed && serial == _completionSerial) Render();
        }

        void BeginDownload()
        {
            if (!isActiveAndEnabled || _destroyed) return;
            Initialize();
            if (!HasAuthoredUi) return;
            float minimum = ClampSeconds(_minimumDownloadSeconds);
            float maximum = Mathf.Max(minimum, ClampSeconds(_maximumDownloadSeconds));
            _controller.Begin(UnityEngine.Random.Range(minimum, maximum));
            Render();
        }

        void HandleCompleted(DocumentDownloadRequest request)
        {
            unchecked { ++_completionSerial; }
            Render();
            Completed?.Invoke(request);
        }

        void Render()
        {
            if (_fill != null)
            {
                Vector2 anchor = _fill.rectTransform.anchorMax;
                anchor.x = _controller.Progress;
                _fill.rectTransform.anchorMax = anchor;
            }
            if (_statusIcon != null)
            {
                _statusIcon.sprite = _controller.IsCompleted ? _checkSprite : _downloadSprite;
                _statusIcon.enabled = true;
                _statusIcon.raycastTarget = false;
            }
        }

        static float ClampSeconds(float seconds) =>
            float.IsNaN(seconds) ? .2f : Mathf.Clamp(seconds, .2f, 1f);

        void OnDestroy()
        {
            _destroyed = true;
            if (_controller != null)
            {
                _controller.CancelPending();
                _controller.Completed -= HandleCompleted;
            }
            if (_boundButton != null) _boundButton.onClick.RemoveListener(BeginDownload);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(DocumentKind kind, Button button, Image fill,
            Image statusIcon, Sprite downloadSprite, Sprite checkSprite,
            float minSeconds, float maxSeconds)
        {
            if (Application.isPlaying || _controller != null)
                throw new InvalidOperationException("Cannot configure during Play mode or after initialization.");
            if (kind != DocumentKind.Application && kind != DocumentKind.Portfolio)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (button == null || fill == null || statusIcon == null ||
                downloadSprite == null || checkSprite == null)
                throw new ArgumentException("Button, fill, status icon and both sprites are required.");
            if (button.gameObject != gameObject)
                throw new ArgumentException("Download item must be attached to its button.", nameof(button));
            _kind = kind;
            _button = button;
            _fill = fill;
            _statusIcon = statusIcon;
            _downloadSprite = downloadSprite;
            _checkSprite = checkSprite;
            _minimumDownloadSeconds = ClampSeconds(minSeconds);
            _maximumDownloadSeconds = Mathf.Max(_minimumDownloadSeconds, ClampSeconds(maxSeconds));
        }
#endif
    }
}
