using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class InterviewApplicantPresenter
    {
        readonly StageManager _stage;
        readonly InterviewCallView _call;
        readonly InterviewDocumentPresenter _documents;
        readonly InterviewNotificationPresenter _notification;
        int _displayedIndex = -1;

        public InterviewApplicantPresenter(StageManager stage, InterviewCallView call,
            InterviewDocumentPresenter documents, InterviewNotificationPresenter notification)
        { _stage = stage; _call = call; _documents = documents; _notification = notification; }

        public string ApplicantKey { get; private set; }
        public PortfolioDisplayState DisplayedPortfolioState { get; private set; }

        public void Clear()
        {
            ApplicantKey = null;
            _documents.Clear();
            _notification.Clear();
            _call.Bind(null);
        }

        public void Render()
        {
            var applicant = _stage.Session.IsFinished ? null : _stage.Session.Current;
            _call.Bind(applicant);
            if (applicant == null) { ApplicantKey = null; return; }
            int index = _stage.Session.CurrentIndex;
            bool changed = _displayedIndex != index;
            if (changed)
            {
                _displayedIndex = index;
                ApplicantKey = _stage.Context.ApplicantKey(index);
                DisplayedPortfolioState = applicant.TruePortfolioQuality;
                _documents.Bind(applicant, ApplicantKey, DisplayedPortfolioState);
                _notification.NewApplicant();
            }
            else _notification.Render(); // Re-enable restores this applicant without rebinding documents.
        }
    }
}