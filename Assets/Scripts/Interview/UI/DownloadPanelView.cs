using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed class DownloadPanelView : MonoBehaviour
    {
        [SerializeField] RectTransform _panelRect;
        [SerializeField] Button _applicationButton;
        [SerializeField] Button _portfolioButton;
        [SerializeField] Button _closeButton;
        [SerializeField] Vector2 _closedPosition = new Vector2(1400, 0);
        [SerializeField] Vector2 _openPosition = Vector2.zero;
        [SerializeField, Min(0f)] float _slideSeconds = .25f;
        [SerializeField] Image _applicationFill;
        [SerializeField] Image _portfolioFill;
        [SerializeField, Range(.2f, 1f)] float _minimumDownloadSeconds = .2f;
        [SerializeField, Range(.2f, 1f)] float _maximumDownloadSeconds = 1f;
        Coroutine _slide;
        readonly DocumentDownloadController _downloads = new DocumentDownloadController();
        public event Action<DocumentDownloadRequest> DownloadCompleted;

        public void ConfigureProgressEditor(Image applicationFill, Image portfolioFill)
        {
            _applicationFill = applicationFill;
            _portfolioFill = portfolioFill;
            RenderProgress();
        }

        public void BindContext(string key) { _downloads.BindContext(key); RenderProgress(); }
        public bool IsCurrent(DocumentDownloadRequest request) => _downloads.IsCurrent(request);
        public void CancelPending() { _downloads.CancelPending(); RenderProgress(); }
        public void TickDownloads(float deltaTime) { _downloads.Tick(deltaTime); RenderProgress(); }

        void RenderProgress()
        {
            SetProgress(_applicationFill, _downloads.Progress(DocumentKind.Application));
            SetProgress(_portfolioFill, _downloads.Progress(DocumentKind.Portfolio));
        }

        void OnDownloadCompleted(DocumentDownloadRequest request)
        {
            DownloadCompleted?.Invoke(request);
            if (!IsCurrent(request)) return;
            if (request.Kind == DocumentKind.Application) ApplicationRequested?.Invoke();
            else PortfolioRequested?.Invoke();
        }

        static void SetProgress(Image fill, float progress)
        {
            if (!fill) return;
            fill.raycastTarget = false;
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        void BeginDownload(DocumentKind kind)
        {
            if (!isActiveAndEnabled) return;
            float minimum = Mathf.Clamp(_minimumDownloadSeconds, .2f, 1f);
            float maximum = Mathf.Clamp(_maximumDownloadSeconds, minimum, 1f);
            _downloads.Begin(kind, UnityEngine.Random.Range(minimum, maximum));
            RenderProgress();
        }
        public event Action ApplicationRequested;
        public event Action PortfolioRequested;
        public bool HasAuthoredUi => _panelRect && _applicationButton && _portfolioButton && _closeButton;
        public void Configure(RectTransform panel, Button application, Button portfolio, Button close, Vector2 closedPosition, Vector2 openPosition, float duration)
        {
            _panelRect = panel; _applicationButton = application; _portfolioButton = portfolio; _closeButton = close;
            _closedPosition = closedPosition; _openPosition = openPosition; _slideSeconds = duration;
        }
        void Awake()
        {
            if (!HasAuthoredUi) { Debug.LogError("DownloadPanelView: missing authored references.", this); enabled = false; return; }
            _downloads.Completed += OnDownloadCompleted;
            _applicationButton.onClick.AddListener(RequestApplication);
            _portfolioButton.onClick.AddListener(RequestPortfolio);
            _closeButton.onClick.AddListener(Close);
        }
        void OnDestroy()
        {
            _downloads.CancelPending();
            _downloads.Completed -= OnDownloadCompleted;
            if (_applicationButton) _applicationButton.onClick.RemoveListener(RequestApplication);
            if (_portfolioButton) _portfolioButton.onClick.RemoveListener(RequestPortfolio);
            if (_closeButton) _closeButton.onClick.RemoveListener(Close);
        }
        void OnEnable() { RenderProgress(); }
        void OnDisable() { CloseImmediate(); }
        void RequestApplication() => BeginDownload(DocumentKind.Application);
        void RequestPortfolio() => BeginDownload(DocumentKind.Portfolio);
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