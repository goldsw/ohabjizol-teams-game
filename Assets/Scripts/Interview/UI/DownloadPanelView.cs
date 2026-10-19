using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed partial class DownloadPanelView : MonoBehaviour
    {
        [SerializeField] RectTransform _panelRect;
        [SerializeField, HideInInspector] Button _applicationButton;
        [SerializeField, HideInInspector] Button _portfolioButton;
        [SerializeField] Button _closeButton;
        [SerializeField] DownloadItemView[] _items = Array.Empty<DownloadItemView>();
        [SerializeField] Vector2 _closedPosition = new Vector2(1400, 0);
        [SerializeField] Vector2 _openPosition = Vector2.zero;
        [SerializeField, Min(0f)] float _slideSeconds = .25f;
        [SerializeField, HideInInspector] Image _applicationFill;
        [SerializeField, HideInInspector] Image _portfolioFill;
        [SerializeField, HideInInspector] float _minimumDownloadSeconds = .2f;
        [SerializeField, HideInInspector] float _maximumDownloadSeconds = 1f;
        Coroutine _slide;
        string _context;
        uint _revision;
        bool _initialized;
        public IReadOnlyList<DownloadItemView> Items => Array.AsReadOnly(_items ?? Array.Empty<DownloadItemView>());
        public event Action<DocumentDownloadRequest> DownloadCompleted;
        public event Action ApplicationRequested;
        public event Action PortfolioRequested;
        public bool HasAuthoredUi => _panelRect && _closeButton && ValidItems();
        bool ValidItems()
        {
            if (_items == null || _items.Length != 2) return false;
            var kinds = new HashSet<DocumentKind>();
            var buttons = new HashSet<Button>();
            foreach (var item in _items)
                if (!item || !item.HasAuthoredUi || !item.transform.IsChildOf(transform) ||
                    (item.Kind != DocumentKind.Application && item.Kind != DocumentKind.Portfolio) ||
                    !kinds.Add(item.Kind) || !buttons.Add(item.Button)) return false;
            return true;
        }
        bool Initialize()
        {
            if (_initialized) return true;
            if (!HasAuthoredUi) return false;
            _initialized = true;
            foreach (var item in _items)
            {
                item.Completed -= OnDownloadCompleted;
                item.Completed += OnDownloadCompleted;
                item.BindContext(_context);
            }
            _closeButton.onClick.AddListener(Close);
            return true;
        }
        public void BindContext(string key)
        {
            if (!string.Equals(_context, key, StringComparison.Ordinal) || key == null) ++_revision;
            _context = key;
            if (!Initialize()) return;
            foreach (var item in _items) if (item) item.BindContext(key);
        }
        public bool IsCurrent(DocumentDownloadRequest request)
        {
            if (request == null || (request.Kind != DocumentKind.Application && request.Kind != DocumentKind.Portfolio) || !Initialize()) return false;
            foreach (var item in _items)
                if (item && item.Kind == request.Kind) return item.IsCurrent(request);
            return false;
        }
        public void CancelPending()
        {
            ++_revision;
            if (!Initialize()) return;
            foreach (var item in _items) if (item) item.CancelPending();
        }
        public void TickDownloads(float deltaTime)
        {
            if (!Initialize()) return;
            uint revision = _revision;
            foreach (var item in _items)
            {
                if (item) item.TickDownloads(deltaTime);
                if (revision != _revision) break;
            }
        }
        void OnDownloadCompleted(DocumentDownloadRequest request)
        {
            if (!IsCurrent(request)) return;
            DownloadCompleted?.Invoke(request);
            if (!IsCurrent(request)) return;
            if (request.Kind == DocumentKind.Application) ApplicationRequested?.Invoke();
            else if (request.Kind == DocumentKind.Portfolio) PortfolioRequested?.Invoke();
        }
        void Awake()
        {
            if (!Initialize()) { Debug.LogError("DownloadPanelView: missing or duplicate authored items.", this); enabled = false; }
        }
        void OnDisable() { CloseImmediate(); }
        void OnDestroy()
        {
            ++_revision;
            if (_items != null) foreach (var item in _items)
            {
                if (!item) continue;
                item.CancelPending();
                item.Completed -= OnDownloadCompleted;
            }
            if (_closeButton) _closeButton.onClick.RemoveListener(Close);
        }
        public void Open() => MoveTo(_openPosition);
        public void Close() { MoveTo(_closedPosition); }
        public void CloseImmediate()
        {
            if (_slide != null) StopCoroutine(_slide);
            _slide = null;
            if (_panelRect) _panelRect.anchoredPosition = _closedPosition;
        }
        void MoveTo(Vector2 target)
        {
            if (!HasAuthoredUi || !isActiveAndEnabled) return;
            if (_slide != null) StopCoroutine(_slide);
            if (_slideSeconds <= 0) { _panelRect.anchoredPosition = target; _slide = null; return; }
            _slide = StartCoroutine(Slide(target));
        }
        IEnumerator Slide(Vector2 target)
        {
            Vector2 start = _panelRect.anchoredPosition;
            float elapsed = 0;
            while (elapsed < _slideSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _panelRect.anchoredPosition = Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / _slideSeconds));
                yield return null;
            }
            _panelRect.anchoredPosition = target;
            _slide = null;
        }
    }
}