using System;
using System.Collections;
using UnityEngine;

namespace Ohabjizol.Interview
{
    // Connects request events to state owners and feature presenters.
    public sealed class InterviewRuntime
    {
        readonly InterviewScreen _screen;
        readonly StageManager _stage;
        readonly QuestionController _questions;
        readonly InterviewQuestionPresenter _questionView;
        readonly InterviewApplicantPresenter _applicant;
        readonly InterviewDocumentPresenter _documents;
        readonly InterviewNotificationPresenter _notification;
        readonly InterviewSummaryPresenter _summary;
        readonly StageCalendarWindow _calendar;
        readonly UnityEngine.UI.Button _calendarButton;
        readonly InterviewDecisionCoordinator _decision;
        Coroutine _downloadTick;
        bool _subscribed;
        bool _started;
        bool _disposed;

        // Recovery and snapshot capture must finish before constructing a new session.
        public static InterviewRuntime Create(InterviewScreen screen, InterviewViewReferences views)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            if (views == null) throw new ArgumentNullException(nameof(views));
            StageCompletionService.RecoverPending();
            string runId = CampaignRunStore.Load().RunId;
            StageDefinition definition = StageCatalog.Get(StageSelection.Current);
            var context = new StageContext(runId, definition);
            int productionDays = views.ScheduleConfig != null ? views.ScheduleConfig.GetProductionDays(definition.Id) : 15;
            var manager = new StageManager(context, StageCompletionService.Complete, productionDays);
            var questions = new QuestionController(screen.FakePortfolioAnswerChance, manager.Session.RandomSource);
            if (views.CalendarWindow != null)
                views.CalendarWindow.Bind(new StageCalendarState(manager.Session.ProjectSchedule), definition.DisplayName);
            views.Workspace.Configure(views.ItemParent, views.WindowParent, views.ItemPrefab,
                views.ViewerPrefab, views.PortfolioCatalog);
            var documents = new InterviewDocumentPresenter(views.Workspace, views.Panel);
            var notification = new InterviewNotificationPresenter(views.Notification, views.UnreadMark);
            var applicant = new InterviewApplicantPresenter(manager, views.Call, documents, notification);
            var questionView = new InterviewQuestionPresenter(screen, questions, views.QuestionCatalog,
                views.Questions, views.Call, views.Response, views.RadialQuestions, applicant);
            var summary = new InterviewSummaryPresenter(manager, views.Title, views.StageLabel, views.Finished,
                views.Current, views.Required, views.Slots, views.Accept, views.Reject, views.Chat, views.ApplicantStats);
            return new InterviewRuntime(screen, manager, questions, questionView, applicant, documents, notification, summary,
                views.CalendarWindow, views.CalendarButton);
        }
        public InterviewRuntime(InterviewScreen screen, StageManager stage, QuestionController questions,
            InterviewQuestionPresenter questionView, InterviewApplicantPresenter applicant,
            InterviewDocumentPresenter documents, InterviewNotificationPresenter notification, InterviewSummaryPresenter summary,
            StageCalendarWindow calendar = null, UnityEngine.UI.Button calendarButton = null)
        {
            _screen = screen; _stage = stage; _questions = questions; _questionView = questionView;
            _applicant = applicant; _documents = documents; _notification = notification; _summary = summary;
            _calendar = calendar; _calendarButton = calendarButton;
            _decision = new InterviewDecisionCoordinator(screen, stage, PresentDecision);
            _decision.Changed += DecisionChanged;
        }
        public string ApplicantKey => _applicant.ApplicantKey;
        public PortfolioDisplayState DisplayedPortfolioState => _applicant.DisplayedPortfolioState;
        public bool IsAnswering => _questions.IsAnswering;
        public int UsedQuestionCount => _questions.UsedQuestionCount;
        public bool InboxUnread => _notification.Unread;
        public InterviewResult Result => _stage.Result;

        public void Enable()
        {
            if (_disposed || _subscribed) return;
            _subscribed = true;
            if (_calendarButton != null) _calendarButton.onClick.AddListener(OpenCalendar);
            _screen.QuestionRequested += Ask;
            _screen.DecisionRequested += _decision.Request;
            _screen.DocumentPanelRequested += OpenDocuments;
            _screen.CompletionRetryRequested += RetryCompletion;
            _stage.ApplicantChanging += ClearApplicant;
            _stage.ApplicantChanged += BindApplicant;
            _stage.StateChanged += _summary.Render;
            _stage.CompletionFailed += ReportCompletionFailure;
            _questionView.Subscribe();
            _documents.Subscribe();
            _downloadTick = _screen.StartCoroutine(TickDownloads());
            if (!_started)
            {
                _started = true;
                _stage.Start();
            }
            else
            {
                _applicant.Render();
                _questionView.Render();
                _summary.Render();
                RetryCompletion();
            }
            DecisionChanged();
            _decision.Resume();
        }

        public void Disable()
        {
            _decision.Suspend();
            if (_downloadTick != null && _screen) _screen.StopCoroutine(_downloadTick);
            _downloadTick = null;
            if (!_subscribed) return;
            _questionView.Cancel();
            _documents.Suspend();
            if (_calendar != null) _calendar.Close();
            if (_calendarButton != null) _calendarButton.onClick.RemoveListener(OpenCalendar);
            _documents.Unsubscribe();
            _questionView.Unsubscribe();
            _screen.QuestionRequested -= Ask;
            _screen.DecisionRequested -= _decision.Request;
            _screen.DocumentPanelRequested -= OpenDocuments;
            _screen.CompletionRetryRequested -= RetryCompletion;
            _stage.ApplicantChanging -= ClearApplicant;
            _stage.ApplicantChanged -= BindApplicant;
            _stage.StateChanged -= _summary.Render;
            _stage.CompletionFailed -= ReportCompletionFailure;
            _subscribed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disable();
            _decision.Changed -= DecisionChanged;
            _decision.Dispose();
            ClearApplicant();
            _documents.Dispose();
        }

        IEnumerator TickDownloads()
        {
            while (_subscribed && !_disposed)
            {
                yield return null;
                if (_subscribed && !_disposed) _documents.TickDownloads(Time.unscaledDeltaTime);
            }
        }

        float PresentDecision(bool accepted)
        {
            _questionView.Cancel();
            return accepted ? _applicant.Accepted() : _applicant.Rejected();
        }

        void DecisionChanged()
        {
            bool blocked = _decision.IsPending;
            if (!blocked) _applicant.CancelDecision();
            _questionView.SetInputBlocked(blocked);
            _summary.SetInputBlocked(blocked);
        }

        void RetryCompletion()
        {
            if (_disposed || !_subscribed || _decision.IsPending) return;
            _stage.RetryCompletion();
        }

        void OpenCalendar()
        {
            if (_disposed || !_subscribed || _decision.IsPending) return;
            if (_calendar != null) _calendar.Restore();
        }

        void Ask(int index)
        {
            if (!_disposed && _subscribed && !_decision.IsPending
                && !_stage.IsDeciding && !_stage.Session.IsFinished
                && _questions.Applicant == _stage.Session.Current) _questionView.Ask(index);
        }

        void OpenDocuments()
        {
            if (_disposed || !_subscribed || _decision.IsPending
                || _stage.IsDeciding || _stage.Session.IsFinished || _stage.Session.Current == null) return;
            _notification.Clear();
            _documents.Open();
        }

        void ClearApplicant()
        {
            if (_calendar != null) _calendar.Close();
            _questionView.Bind(null);
            _applicant.Clear();
        }

        void BindApplicant()
        {
            _applicant.Render();
            _questionView.Bind(_stage.Session.IsFinished ? null : _stage.Session.Current);
        }

        void ReportCompletionFailure(Exception error) => Debug.LogError(
            "Interview completion is pending; retry with RetryCompletion or a decision button. " + error.Message, _screen);
    }
}