using UnityEngine;

namespace Ohabjizol.Interview
{
    public enum ApplicantMotionState { Idle, Talk, Accepted, Rejected }

    public sealed class InterviewApplicantPresenter
    {
        readonly StageManager _stage;
        readonly InterviewCallView _call;
        readonly InterviewDocumentPresenter _documents;
        readonly InterviewNotificationPresenter _notification;
        int _displayedIndex = -1;
        Applicant _displayedApplicant;
        ApplicantVisualDefinition _visual;

        public InterviewApplicantPresenter(StageManager stage, InterviewCallView call,
            InterviewDocumentPresenter documents, InterviewNotificationPresenter notification)
        { _stage = stage; _call = call; _documents = documents; _notification = notification; }

        public string ApplicantKey { get; private set; }
        public PortfolioDisplayState DisplayedPortfolioState { get; private set; }
        public ApplicantMotionState State { get; private set; } = ApplicantMotionState.Idle;

        public void Clear()
        {
            ApplicantKey = null;
            _displayedIndex = -1;
            _displayedApplicant = null;
            _visual = null;
            State = ApplicantMotionState.Idle;
            DisplayedPortfolioState = default(PortfolioDisplayState);
            _documents.Clear();
            _notification.Clear();
            _call.Bind(null);
            _call.SetPose(null);
        }

        public void Render()
        {
            var applicant = _stage.Session.IsFinished ? null : _stage.Session.Current;
            if (applicant == null)
            {
                if (_displayedApplicant != null) Clear();
                else
                {
                    _call.Bind(null);
                    _call.SetPose(null);
                }
                return;
            }
            int index = _stage.Session.CurrentIndex;
            if (_displayedIndex == index && ReferenceEquals(_displayedApplicant, applicant)) return;

            _displayedIndex = index;
            _displayedApplicant = applicant;
            ApplicantKey = _stage.Context.ApplicantKey(index);
            DisplayedPortfolioState = applicant.TruePortfolioQuality;
            _visual = null;
            var catalog = _call.VisualCatalog;
            if (!catalog || !catalog.TryGet(applicant.CharacterId, out _visual))
                Debug.LogWarning($"Applicant visual mapping missing or ambiguous: '{applicant.CharacterId ?? "<null>"}'.", _call);
            else if (!_visual.Idle || !_visual.Talk || !_visual.Accepted || !_visual.Rejected)
                Debug.LogWarning($"Applicant visual has missing poses: '{applicant.CharacterId}'.", _call);
            _call.Bind(applicant);
            State = ApplicantMotionState.Idle;
            _call.SetPose(_visual?.Idle);
            _documents.Bind(applicant, ApplicantKey, DisplayedPortfolioState);
            _notification.NewApplicant();
        }

        public void SetAnswering(bool answering, int tick)
        {
            if (State == ApplicantMotionState.Accepted || State == ApplicantMotionState.Rejected) return;
            State = answering && (tick % 2) != 0 ? ApplicantMotionState.Talk : ApplicantMotionState.Idle;
            _call.SetPose(State == ApplicantMotionState.Talk ? _visual?.Talk : _visual?.Idle);
        }

        public float Accepted() => PresentDecision(ApplicantMotionState.Accepted);
        public float Rejected() => PresentDecision(ApplicantMotionState.Rejected);

        float PresentDecision(ApplicantMotionState state)
        {
            State = state;
            _call.SetAnswering(false, 0);
            bool accepted = state == ApplicantMotionState.Accepted;
            _call.SetPose(accepted ? _visual?.Accepted : _visual?.Rejected);
            float duration = (accepted ? _visual?.AcceptedDuration : _visual?.RejectedDuration) ?? 3f;
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f) duration = 3f;
            return _call.BeginDeparture(accepted ? "감사합니다!" : "다음에는 꼭..!", accepted ? 1f : duration);
        }

        public void CancelDecision()
        {
            State = ApplicantMotionState.Idle;
            _call.CancelDeparture();
            _call.SetAnswering(false, 0);
            _call.SetPose(_visual?.Idle);
        }
    }
}
