using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen : MonoBehaviour
    {
        public const string ResultSceneName = "ResultScene";
        [Header("Authored view")]
        [SerializeField] RectTransform _discordViewRoot;
        [SerializeField] Button _discordButton;
        [SerializeField] Button _acceptButton;
        [SerializeField] Button _rejectButton;
        [SerializeField] Button _chatButton;
        [SerializeField] GameObject _unreadMark;
        [SerializeField] Text _notification;
        [SerializeField] Text _response;
        [SerializeField] Text _titleLabel;
        [SerializeField] Text _stageLabel;
        [SerializeField] Text _finishedLabel;
        [SerializeField] Text _currentLabel;
        [SerializeField] Text _requiredLabel;
        [SerializeField] ApplicantStatsView _applicantStats;
        [SerializeField] Text[] _slotLabels = new Text[4];
        [SerializeField] InterviewQuestionView[] _questions = new InterviewQuestionView[5];
        [SerializeField] InterviewCallView _call;
        [SerializeField] RadialQuestionView _radialQuestions;
        [SerializeField] DownloadPanelView _downloadPanel;
        [Header("Document workspace")]
        [SerializeField] DocumentWorkspace _workspace;
        [SerializeField] RectTransform _itemParent;
        [SerializeField] RectTransform _windowParent;
        [SerializeField] DocumentTaskbarItem _itemPrefab;
        [SerializeField] PdfDocumentViewer _viewerPrefab;
        [SerializeField] PortfolioSpriteCatalog _portfolioCatalog;
        [SerializeField] AuthoredQuestionCatalog _questionCatalog;
        [Header("Stage calendar")]
        [SerializeField] StageCalendarWindow _calendarWindow;
        [SerializeField] StageScheduleConfig _scheduleConfig;
        [SerializeField] Button _calendarButton;
        [SerializeField, Range(0f, 1f)] float fakePortfolioAnswerChance = .5f;
        public float FakePortfolioAnswerChance => fakePortfolioAnswerChance;
        [SerializeField, HideInInspector] bool discordVisible = true;
        InterviewRuntime _runtime;
        readonly UnityEngine.Events.UnityAction[] _questionHandlers = new UnityEngine.Events.UnityAction[5];
        bool _inputsSubscribed;
        public event Action<int> QuestionRequested;
        public event Action<bool> DecisionRequested;
        public event Action DocumentPanelRequested;
        public event Action CompletionRetryRequested;

        public string ApplicantKey => _runtime?.ApplicantKey;
        public PortfolioDisplayState DisplayedPortfolioState => _runtime == null ? default(PortfolioDisplayState) : _runtime.DisplayedPortfolioState;
        public bool IsAnswering => _runtime != null && _runtime.IsAnswering;
        public int UsedQuestionCount => _runtime == null ? 0 : _runtime.UsedQuestionCount;
        public DocumentWorkspace Documents => _workspace;
        public bool InboxUnread => _runtime != null && _runtime.InboxUnread;
        public void AskQuestion(int index) { if (isActiveAndEnabled) QuestionRequested?.Invoke(index); }
        public void AcceptApplicant() { if (isActiveAndEnabled) DecisionRequested?.Invoke(true); }
        public void RejectApplicant() { if (isActiveAndEnabled) DecisionRequested?.Invoke(false); }
        public void OpenDocumentPanel() { if (isActiveAndEnabled) DocumentPanelRequested?.Invoke(); }
        public void RetryCompletion() { if (isActiveAndEnabled) CompletionRetryRequested?.Invoke(); }
        public string HandoffSceneName => Result != null ? ResultSceneName : null;
        public InterviewResult Result => _runtime?.Result;
        public bool HasAuthoredUi
        {
            get
            {
                if (!_discordViewRoot || !_discordButton || !_acceptButton || !_rejectButton || !_chatButton
                    || !_unreadMark || !_notification || !_response || !_titleLabel || !_stageLabel || !_finishedLabel
                    || !_currentLabel || !_requiredLabel || !_call || !_call.HasAuthoredUi
                    || !_downloadPanel || !_downloadPanel.HasAuthoredUi || !_workspace || !_itemParent
                    || !_windowParent || !_itemPrefab || !_viewerPrefab || !_portfolioCatalog || !_questionCatalog
                    || !_workspace.HasAuthoredLibrary || !_calendarWindow || !_scheduleConfig || !_calendarButton
                    || _questions == null || _questions.Length != 5 || _slotLabels == null || _slotLabels.Length != 4)
                    return false;
                foreach (var question in _questions) if (!question || !question.HasAuthoredUi) return false;
                foreach (var slot in _slotLabels) if (!slot) return false;
                return true;
            }
        }
        void Awake()
        {
            if (!HasAuthoredUi)
            {
                Debug.LogError("InterviewScreen: authored UI references/templates missing. Run BakeAuthoredUi in EditMode and save the prefab/scene.", this);
                enabled = false;
                return;
            }
            try
            {
                var views = new InterviewViewReferences(_call, _questions, _questionCatalog, _response,
                    _workspace, _itemParent, _windowParent, _itemPrefab, _viewerPrefab, _portfolioCatalog, _downloadPanel,
                    _notification, _unreadMark, _titleLabel, _stageLabel, _finishedLabel, _currentLabel,
                    _requiredLabel, _slotLabels, _acceptButton, _rejectButton, _chatButton,
                    _calendarWindow, _scheduleConfig, _calendarButton, _radialQuestions, _applicantStats);
                _runtime = InterviewRuntime.Create(this, views);
                discordVisible = _discordViewRoot.gameObject.activeSelf;
                for (int i = 0; i < _questionHandlers.Length; i++)
                {
                    int index = i;
                    _questionHandlers[i] = () => AskQuestion(index);
                }
            }
            catch (Exception error)
            {
                Debug.LogError("InterviewScreen entry failed: " + error, this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (_runtime == null) return;
            try
            {
                _runtime.Enable();
                SubscribeInputs();
            }
            catch (Exception error)
            {
                Debug.LogError("InterviewScreen activation failed: " + error, this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            UnsubscribeInputs();
            _runtime?.Disable();
        }

        void OnDestroy()
        {
            UnsubscribeInputs();
            _runtime?.Dispose();
            _runtime = null;
        }

        void SubscribeInputs()
        {
            if (_inputsSubscribed) return;
            _inputsSubscribed = true;
            _acceptButton.onClick.AddListener(AcceptApplicant);
            _rejectButton.onClick.AddListener(RejectApplicant);
            _chatButton.onClick.AddListener(OpenDocumentPanel);
            _discordButton.onClick.AddListener(ToggleDiscord);
            for (int i = 0; i < _questions.Length; i++)
                _questions[i].Button.onClick.AddListener(_questionHandlers[i]);
        }

        void UnsubscribeInputs()
        {
            if (!_inputsSubscribed) return;
            _inputsSubscribed = false;
            if (_acceptButton) _acceptButton.onClick.RemoveListener(AcceptApplicant);
            if (_rejectButton) _rejectButton.onClick.RemoveListener(RejectApplicant);
            if (_chatButton) _chatButton.onClick.RemoveListener(OpenDocumentPanel);
            if (_discordButton) _discordButton.onClick.RemoveListener(ToggleDiscord);
            for (int i = 0; i < _questionHandlers.Length; i++)
                if (_questions != null && i < _questions.Length && _questions[i] && _questionHandlers[i] != null)
                    _questions[i].Button.onClick.RemoveListener(_questionHandlers[i]);
        }

        void ToggleDiscord() => SetDiscordVisible(!discordVisible);
        public void SetDiscordVisible(bool visible)
        {
            discordVisible = visible;
            if (_discordViewRoot) _discordViewRoot.gameObject.SetActive(visible);
        }
    }
}
