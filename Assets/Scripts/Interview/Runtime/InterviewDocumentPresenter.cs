using System;

namespace Ohabjizol.Interview
{
    public sealed class InterviewDocumentPresenter
    {
        readonly DocumentWorkspace _workspace;
        readonly DownloadPanelView _panel;
        bool _hasApplicant;
        bool _subscribed;
        string _currentKey;

        public InterviewDocumentPresenter(DocumentWorkspace workspace, DownloadPanelView panel)
        { _workspace = workspace; _panel = panel; }

        public void Subscribe()
        {
            if (_subscribed) return;
            _panel.DownloadCompleted += OnDownloadCompleted;
            _subscribed = true;
        }

        public void Unsubscribe()
        {
            _panel.CancelPending();
            _panel.DownloadCompleted -= OnDownloadCompleted;
            _subscribed = false;
        }

        public void Bind(Applicant applicant, string key, PortfolioDisplayState state)
        {
            _hasApplicant = applicant != null;
            _currentKey = _hasApplicant ? key : null;
            _panel.BindContext(_currentKey);
            _workspace.BindApplicant(applicant == null ? null : ApplicantPresentationFactory.Create(applicant, key, state));
        }

        public void TickDownloads(float deltaTime) { if (_panel) _panel.TickDownloads(deltaTime); }

        public void Open() { if (_hasApplicant) _panel.Open(); }
        public void Suspend() { _panel.CloseImmediate(); _workspace.CloseWindows(); }
        public void Clear()
        {
            _hasApplicant = false;
            _currentKey = null;
            _panel.BindContext(null);
            _workspace.ClearApplicant();
            _panel.CloseImmediate();
        }
        public void Dispose() { Unsubscribe(); Clear(); _workspace.Dispose(); }

        void OnDownloadCompleted(DocumentDownloadRequest request)
        {
            if (!_hasApplicant || !_panel.IsCurrent(request) ||
                !string.Equals(_currentKey, request.ApplicantKey, StringComparison.Ordinal)) return;
            _workspace.Download(request.Kind);
        }
    }
}