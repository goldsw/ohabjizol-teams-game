using System;

namespace Ohabjizol.Interview
{
    // Download state survives changes to panel visibility.
    public sealed class DocumentDownloadController
    {
        sealed class Job
        {
            public DocumentDownloadRequest Request;
            public float Elapsed, Duration, Progress;
            public bool Completed;
        }

        readonly Job[] _jobs = { new Job(), new Job() };
        string _key;
        int _generation;
        public event Action<DocumentDownloadRequest> Completed;

        static int Index(DocumentKind kind) => kind == DocumentKind.Application ? 0 : 1;
        public float Progress(DocumentKind kind) => _jobs[Index(kind)].Progress;
        public bool IsCurrent(DocumentDownloadRequest request) => request != null &&
            request.Generation == _generation &&
            string.Equals(request.ApplicantKey, _key, StringComparison.Ordinal);

        public void BindContext(string key)
        {
            if (string.Equals(_key, key, StringComparison.Ordinal)) return;
            CancelPending();
            _key = key;
            foreach (Job job in _jobs)
            {
                job.Completed = false;
                job.Progress = 0f;
            }
        }

        public void CancelPending()
        {
            unchecked { ++_generation; }
            foreach (Job job in _jobs)
            {
                job.Request = null;
                job.Elapsed = 0f;
                job.Progress = job.Completed ? 1f : 0f;
            }
        }

        public void Begin(DocumentKind kind, float duration)
        {
            if (string.IsNullOrEmpty(_key)) return;
            Job job = _jobs[Index(kind)];
            if (job.Request != null || job.Completed) return;
            job.Request = new DocumentDownloadRequest(_key, _generation, kind);
            job.Elapsed = job.Progress = 0f;
            job.Duration = Math.Max(.2f, Math.Min(1f, duration));
        }

        public void Tick(float deltaTime)
        {
            int generation = _generation;
            foreach (Job job in _jobs)
            {
                if (generation != _generation) return;
                if (job.Request == null) continue;
                job.Elapsed += Math.Max(0f, deltaTime);
                job.Progress = Math.Min(1f, job.Elapsed / job.Duration);
                if (job.Progress < 1f) continue;
                DocumentDownloadRequest request = job.Request;
                job.Request = null;
                job.Completed = true;
                if (IsCurrent(request)) Completed?.Invoke(request);
            }
        }
    }
}
