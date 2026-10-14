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
        Coroutine _applicationDownload, _portfolioDownload;
        bool _applicationPending, _portfolioPending, _applicationCompleted, _portfolioCompleted;
        string _applicantKey;
        int _generation;
        public event Action<DocumentDownloadRequest> DownloadCompleted;

        public void ConfigureProgressEditor(Image applicationFill, Image portfolioFill)
        {
            _applicationFill = applicationFill;
            _portfolioFill = portfolioFill;
            SetProgress(_applicationFill, _applicationCompleted ? 1f : 0f);
            SetProgress(_portfolioFill, _portfolioCompleted ? 1f : 0f);
        }

        public void BindContext(string key)
        {
            CancelPending();
            if (!string.Equals(_applicantKey, key, StringComparison.Ordinal))
            {
                _applicationCompleted = _portfolioCompleted = false;
                SetProgress(_applicationFill, 0f);
                SetProgress(_portfolioFill, 0f);
            }
            _applicantKey = key;
        }

        public bool IsCurrent(DocumentDownloadRequest request) =>
            request != null && request.Generation == _generation &&
            string.Equals(request.ApplicantKey, _applicantKey, StringComparison.Ordinal);

        public void CancelPending()
        {
            unchecked { ++_generation; }
            if (_applicationDownload != null) StopCoroutine(_applicationDownload);
            if (_portfolioDownload != null) StopCoroutine(_portfolioDownload);
            _applicationDownload = _portfolioDownload = null;
            _applicationPending = _portfolioPending = false;
            SetProgress(_applicationFill, _applicationCompleted ? 1f : 0f);
            SetProgress(_portfolioFill, _portfolioCompleted ? 1f : 0f);
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
            if (!isActiveAndEnabled || string.IsNullOrEmpty(_applicantKey)) return;
            bool application = kind == DocumentKind.Application;
            if (application ? _applicationPending || _applicationCompleted : _portfolioPending || _portfolioCompleted) return;
            var request = new DocumentDownloadRequest(_applicantKey, _generation, kind);
            float minimum = Mathf.Clamp(_minimumDownloadSeconds, .2f, 1f);
            float maximum = Mathf.Clamp(_maximumDownloadSeconds, minimum, 1f);
            if (application)
            {
                _applicationPending = true;
                _applicationDownload = StartCoroutine(Download(request, UnityEngine.Random.Range(minimum, maximum)));
            }
            else
            {
                _portfolioPending = true;
                _portfolioDownload = StartCoroutine(Download(request, UnityEngine.Random.Range(minimum, maximum)));
            }
        }

        IEnumerator Download(DocumentDownloadRequest request, float duration)
        {
            bool application = request.Kind == DocumentKind.Application;
            Image fill = application ? _applicationFill : _portfolioFill;
            float elapsed = 0f;
            SetProgress(fill, 0f);
            while (elapsed < duration)
            {
                yield return null;
                if (!IsCurrent(request)) yield break;
                elapsed += Time.unscaledDeltaTime;
                SetProgress(fill, elapsed / duration);
            }
            if (!IsCurrent(request)) yield break;
            SetProgress(fill, 1f);
            if (application)
            {
                _applicationPending = false;
                _applicationCompleted = true;
                _applicationDownload = null;
            }
            else
            {
                _portfolioPending = false;
                _portfolioCompleted = true;
                _portfolioDownload = null;
            }
            DownloadCompleted?.Invoke(request);
            if (!IsCurrent(request)) yield break;
            if (application) ApplicationRequested?.Invoke();
            else PortfolioRequested?.Invoke();
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
            _applicationButton.onClick.AddListener(RequestApplication);
            _portfolioButton.onClick.AddListener(RequestPortfolio);
            _closeButton.onClick.AddListener(Close);
        }
        void OnDestroy()
        {
            if (_applicationButton) _applicationButton.onClick.RemoveListener(RequestApplication);
            if (_portfolioButton) _portfolioButton.onClick.RemoveListener(RequestPortfolio);
            if (_closeButton) _closeButton.onClick.RemoveListener(Close);
        }
        void OnDisable() { CloseImmediate(); }
        void RequestApplication() => BeginDownload(DocumentKind.Application);
        void RequestPortfolio() => BeginDownload(DocumentKind.Portfolio);
        public void Open() => MoveTo(_openPosition);
        public void Close() { CancelPending(); MoveTo(_closedPosition); }
        public void CloseImmediate()
        {
            CancelPending();
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