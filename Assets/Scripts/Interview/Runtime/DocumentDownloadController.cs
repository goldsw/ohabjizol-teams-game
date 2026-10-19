using System;

namespace Ohabjizol.Interview
{
    public sealed class DocumentDownloadController
    {
        readonly DocumentKind _kind;
        string _key;
        int _generation;
        DocumentDownloadRequest _request;
        float _elapsed, _duration;
        public float Progress { get; private set; }
        public bool IsCompleted { get; private set; }
        public event Action<DocumentDownloadRequest> Completed;

        public DocumentDownloadController(DocumentKind kind)
        {
            if (kind != DocumentKind.Application && kind != DocumentKind.Portfolio)
                throw new ArgumentOutOfRangeException(nameof(kind));
            _kind = kind;
        }

        public bool IsCurrent(DocumentDownloadRequest request) => request != null &&
            request.Kind == _kind && request.Generation == _generation &&
            string.Equals(request.ApplicantKey, _key, StringComparison.Ordinal);

        public void BindContext(string key)
        {
            if (string.Equals(_key, key, StringComparison.Ordinal)) return;
            CancelPending();
            _key = key;
            IsCompleted = false;
            Progress = 0f;
        }

        public void CancelPending()
        {
            unchecked { ++_generation; }
            _request = null;
            _elapsed = _duration = 0f;
            Progress = IsCompleted ? 1f : 0f;
        }

        public void Begin(float duration)
        {
            if (string.IsNullOrEmpty(_key) || _request != null || IsCompleted) return;
            _request = new DocumentDownloadRequest(_key, _generation, _kind);
            _elapsed = Progress = 0f;
            _duration = float.IsNaN(duration) ? .2f : Math.Max(.2f, Math.Min(1f, duration));
        }

        public void Tick(float deltaTime)
        {
            if (_request == null) return;
            if (float.IsNaN(deltaTime)) deltaTime = 0f;
            _elapsed += Math.Max(0f, deltaTime);
            Progress = Math.Min(1f, _elapsed / _duration);
            if (Progress < 1f) return;
            DocumentDownloadRequest request = _request;
            _request = null;
            IsCompleted = true;
            if (IsCurrent(request)) Completed?.Invoke(request);
        }
    }
}
