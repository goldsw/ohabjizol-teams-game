using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    // Immutable reference bundle. Copies protect the authored reference arrays from replacement.
    public sealed class InterviewViewReferences
    {
        readonly InterviewQuestionView[] _questions;
        readonly Text[] _slots;

        public InterviewViewReferences(InterviewCallView call, InterviewQuestionView[] questions,
            AuthoredQuestionCatalog questionCatalog, Text response, DocumentWorkspace workspace,
            RectTransform itemParent, RectTransform windowParent, DocumentTaskbarItem itemPrefab,
            PdfDocumentViewer viewerPrefab, PortfolioSpriteCatalog portfolioCatalog, DownloadPanelView panel, Text notification,
            GameObject unreadMark, Text title, Text stageLabel, Text finished, Text current, Text required,
            Text[] slots, Button accept, Button reject, Button chat,
            StageCalendarWindow calendarWindow = null, StageScheduleConfig scheduleConfig = null, Button calendarButton = null, RadialQuestionView radialQuestions = null)
        {
            Call = call;
            RadialQuestions = radialQuestions;
            _questions = (InterviewQuestionView[])(questions ?? throw new ArgumentNullException(nameof(questions))).Clone();
            QuestionCatalog = questionCatalog;
            Response = response;
            Workspace = workspace;
            ItemParent = itemParent;
            WindowParent = windowParent;
            ItemPrefab = itemPrefab;
            ViewerPrefab = viewerPrefab;
            PortfolioCatalog = portfolioCatalog;
            Panel = panel;
            Notification = notification;
            UnreadMark = unreadMark;
            Title = title;
            StageLabel = stageLabel;
            Finished = finished;
            Current = current;
            Required = required;
            _slots = (Text[])(slots ?? throw new ArgumentNullException(nameof(slots))).Clone();
            Accept = accept;
            Reject = reject;
            Chat = chat;
            CalendarWindow = calendarWindow; ScheduleConfig = scheduleConfig; CalendarButton = calendarButton;
        }

        public InterviewCallView Call { get; }
        public RadialQuestionView RadialQuestions { get; }
        public InterviewQuestionView[] Questions => (InterviewQuestionView[])_questions.Clone();
        public AuthoredQuestionCatalog QuestionCatalog { get; }
        public Text Response { get; }
        public DocumentWorkspace Workspace { get; }
        public RectTransform ItemParent { get; }
        public RectTransform WindowParent { get; }
        public DocumentTaskbarItem ItemPrefab { get; }
        public PdfDocumentViewer ViewerPrefab { get; }
        public PortfolioSpriteCatalog PortfolioCatalog { get; }
        public DownloadPanelView Panel { get; }
        public Text Notification { get; }
        public GameObject UnreadMark { get; }
        public Text Title { get; }
        public Text StageLabel { get; }
        public Text Finished { get; }
        public Text Current { get; }
        public Text Required { get; }
        public Text[] Slots => (Text[])_slots.Clone();
        public Button Accept { get; }
        public Button Reject { get; }
        public Button Chat { get; }
        public StageCalendarWindow CalendarWindow { get; }
        public StageScheduleConfig ScheduleConfig { get; }
        public Button CalendarButton { get; }
    }
}