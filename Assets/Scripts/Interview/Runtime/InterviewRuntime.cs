using System;
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
        bool _subscribed;
        bool _started;

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
                views.Questions, views.Call, views.Response, views.RadialQuestions);
            var summary = new InterviewSummaryPresenter(manager, views.Title, views.StageLabel, views.Finished,
                views.Current, views.Required, views.Slots, views.Accept, views.Reject, views.Chat);
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
        }
        public string ApplicantKey => _applicant.ApplicantKey;
        public PortfolioDisplayState DisplayedPortfolioState => _applicant.DisplayedPortfolioState;
        public bool IsAnswering => _questions.IsAnswering;
        public int UsedQuestionCount => _questions.UsedQuestionCount;
        public bool InboxUnread => _notification.Unread;
        public InterviewResult Result => _stage.Result;

        public void Enable()
        {
            if (_subscribed) return;
            _subscribed = true;
            if (_calendarButton != null) _calendarButton.onClick.AddListener(OpenCalendar);
            _screen.QuestionRequested += Ask;
            _screen.DecisionRequested += _stage.Decide;
            _screen.DocumentPanelRequested += OpenDocuments;
            _screen.CompletionRetryRequested += _stage.RetryCompletion;
            _stage.ApplicantChanging += ClearApplicant;
            _stage.ApplicantChanged += BindApplicant;
            _stage.StateChanged += _summary.Render;
            _stage.CompletionFailed += ReportCompletionFailure;
            _questionView.Subscribe();
            _documents.Subscribe();
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
                _stage.RetryCompletion();
            }
        }

        public void Disable()
        {
            if (!_subscribed) return;
            _questionView.Cancel();
            _documents.Suspend();
            if (_calendar != null) _calendar.Close();
            if (_calendarButton != null) _calendarButton.onClick.RemoveListener(OpenCalendar);
            _documents.Unsubscribe();
            _questionView.Unsubscribe();
            _screen.QuestionRequested -= Ask;
            _screen.DecisionRequested -= _stage.Decide;
            _screen.DocumentPanelRequested -= OpenDocuments;
            _screen.CompletionRetryRequested -= _stage.RetryCompletion;
            _stage.ApplicantChanging -= ClearApplicant;
            _stage.ApplicantChanged -= BindApplicant;
            _stage.StateChanged -= _summary.Render;
            _stage.CompletionFailed -= ReportCompletionFailure;
            _subscribed = false;
        }

        public void Dispose() { Disable(); ClearApplicant(); _documents.Dispose(); }

        void OpenCalendar() { if (_calendar != null) _calendar.Restore(); }

        void Ask(int index)
        {
            if (!_stage.IsDeciding && !_stage.Session.IsFinished
                && _questions.Applicant == _stage.Session.Current) _questionView.Ask(index);
        }

        void OpenDocuments()
        {
            if (_stage.IsDeciding || _stage.Session.IsFinished || _stage.Session.Current == null) return;
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