using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Screen-space interview board. Decision buttons call <see cref="InterviewSession"/> only.
    /// Team faces read displayed skills from the session roster.
    /// When the session ends, <see cref="Result"/> reads that session result. This screen does not open another scene.
    /// </summary>
    public sealed class InterviewScreen : MonoBehaviour
    {
        public const string ResultSceneName = "ResultScene";
        const int SlotCount = 4;
        const int LineSlotCount = 2;

        static readonly Color ApplicationColor = new Color(0.22f, 0.40f, 0.78f, 1f);
        static readonly Color PortfolioColor = new Color(0.82f, 0.45f, 0.14f, 1f);
        static readonly Color ScheduleColor = new Color(0.12f, 0.55f, 0.50f, 1f);
        static readonly Color InterviewRecordColor = new Color(0.48f, 0.30f, 0.68f, 1f);
        static readonly Color SuspicionColor = new Color(0.75f, 0.62f, 0.16f, 1f);
        static readonly Color SuspicionSelectedColor = new Color(0.98f, 0.86f, 0.22f, 1f);
        static readonly Color AcceptColor = new Color(0.16f, 0.58f, 0.28f, 1f);
        static readonly Color RejectColor = new Color(0.62f, 0.16f, 0.20f, 1f);
        static readonly Color PaperColor = new Color(0.93f, 0.95f, 0.92f, 1f);

        static Font _font;
        static Sprite _whiteSprite;

        InterviewSession _session;
        int _documentIndex;
        readonly string[] _lineIds = new string[LineSlotCount];

        Text _themeLabel;
        Text _finishedLabel;
        Text _applicantLabel;
        Text _suspicionLabel;
        Text _sumLabel;
        Text _requiredLabel;
        readonly Button[] _documentButtons = new Button[4];
        readonly Text[] _documentLabels = new Text[4];
        readonly Button[] _lineButtons = new Button[LineSlotCount];
        readonly Text[] _lineLabels = new Text[LineSlotCount];
        readonly Text[] _slotLabels = new Text[SlotCount];
        Button _acceptButton;
        Button _rejectButton;

        public string HandoffSceneName
        {
            get
            {
                if (_session == null || !_session.IsFinished)
                    return null;
                return ResultSceneName;
            }
        }

        public InterviewResult Result
        {
            get
            {
                if (_session == null || !_session.IsFinished)
                    return null;
                return _session.Result;
            }
        }

        void Awake()
        {
            _session = new InterviewSession(PrototypeCatalog.Theme, PrototypeCatalog.Applicants);
            EnsureEventSystem();
            BuildOverlay();
            Refresh();
        }

        void OnDocument(int index)
        {
            _documentIndex = index;
            Refresh();
        }

        void OnLine(int index)
        {
            if (_session == null || index < 0 || index >= _lineIds.Length)
                return;

            string lineId = _lineIds[index];
            if (string.IsNullOrEmpty(lineId))
                return;

            _session.SelectLine(lineId);
            Refresh();
        }

        void OnAccept()
        {
            if (_session == null)
                return;

            _session.Accept();
            Refresh();
        }

        void OnReject()
        {
            if (_session == null)
                return;

            _session.Reject();
            Refresh();
        }

        void Refresh()
        {
            if (_session == null)
                return;

            GameJamTheme theme = _session.Theme;
            _themeLabel.text = "주제  " + theme.DisplayName
                + "    핵심  " + JobRoleText.Korean(theme.CoreRole);
            _finishedLabel.gameObject.SetActive(_session.IsFinished);

            Applicant current = _session.Current;
            bool open = !_session.IsFinished && current != null;
            WriteApplicant(current);
            WriteDocuments(current);
            WriteLines(current, open);
            WriteSlots();
            WriteTotals(theme);
            _suspicionLabel.text = SuspicionText(current);
            _acceptButton.interactable = open;
            _rejectButton.interactable = open;
        }

        void WriteApplicant(Applicant current)
        {
            if (current == null || current.DisplayedSkills == null)
            {
                _applicantLabel.text = "지원자\n없음";
                return;
            }

            SkillSet shown = current.DisplayedSkills;
            _applicantLabel.text = "지원자\n" + current.Name
                + "\n주 직군  " + JobRoleText.Korean(current.PrimaryRole)
                + "\n" + SkillLine("표시 능력", shown.Planning, shown.Programming, shown.Graphics);
        }

        void WriteDocuments(Applicant current)
        {
            int documentCount = current == null ? 0 : current.Documents.Count;
            if (documentCount == 0)
                _documentIndex = 0;
            else if (_documentIndex < 0 || _documentIndex >= documentCount)
                _documentIndex = 0;

            for (int i = 0; i < _documentButtons.Length; i++)
            {
                bool visible = current != null && i < documentCount;
                _documentButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                DocumentKind kind = current.Documents[i].Kind;
                _documentLabels[i].text = DocumentLabel(kind);
                Color color = DocumentColor(kind);
                if (i == _documentIndex)
                    color = Color.Lerp(color, Color.white, 0.35f);
                Paint(_documentButtons[i], color);
            }
        }

        void WriteLines(Applicant current, bool interactive)
        {
            for (int i = 0; i < LineSlotCount; i++)
            {
                _lineIds[i] = null;
                _lineButtons[i].gameObject.SetActive(false);
            }

            if (current == null || current.Documents.Count == 0)
                return;

            IReadOnlyList<DocumentLine> lines = current.Documents[_documentIndex].Lines;
            int count = lines.Count < LineSlotCount ? lines.Count : LineSlotCount;
            string selected = _session.SelectedLineId;
            for (int i = 0; i < count; i++)
            {
                DocumentLine line = lines[i];
                if (line == null)
                    continue;

                bool chosen = !string.IsNullOrEmpty(selected) && line.Id == selected;
                _lineIds[i] = line.Id;
                _lineLabels[i].text = chosen ? "의심  " + line.Sentence : line.Sentence;
                _lineLabels[i].color = Color.black;
                Paint(_lineButtons[i], chosen ? SuspicionSelectedColor : SuspicionColor);
                _lineButtons[i].interactable = interactive;
                _lineButtons[i].gameObject.SetActive(true);
            }
        }

        void WriteSlots()
        {
            IReadOnlyList<TeamMember> members = _session.Team.Members;
            int count = members == null ? 0 : members.Count;
            for (int i = 0; i < SlotCount; i++)
            {
                TeamMember member = i < count ? members[i] : null;
                Applicant applicant = member == null ? null : member.Applicant;
                SkillSet shown = applicant == null ? null : applicant.DisplayedSkills;
                if (applicant == null || shown == null || string.IsNullOrEmpty(applicant.Name))
                {
                    _slotLabels[i].text = (i + 1) + "  빈 자리";
                    continue;
                }

                _slotLabels[i].text = (i + 1) + "  " + applicant.Name
                    + "  " + JobRoleText.Korean(applicant.PrimaryRole)
                    + "\n" + SkillLine("표시", shown.Planning, shown.Programming, shown.Graphics);
            }
        }

        void WriteTotals(GameJamTheme theme)
        {
            SkillSet shown = _session.Team.DisplayedSum;
            SkillSet required = theme != null ? theme.RequiredSkills : null;
            if (shown == null)
                _sumLabel.text = SkillLine("표시 합", 0, 0, 0);
            else
                _sumLabel.text = SkillLine("표시 합", shown.Planning, shown.Programming, shown.Graphics);

            if (required == null)
                _requiredLabel.text = SkillLine("요구", 0, 0, 0);
            else
                _requiredLabel.text = SkillLine("요구", required.Planning, required.Programming, required.Graphics);
        }

        string SuspicionText(Applicant current)
        {
            string selected = _session.SelectedLineId;
            if (current == null || string.IsNullOrEmpty(selected))
                return "의심  없음";

            IReadOnlyList<ApplicantDocument> documents = current.Documents;
            for (int i = 0; i < documents.Count; i++)
            {
                IReadOnlyList<DocumentLine> lines = documents[i].Lines;
                for (int j = 0; j < lines.Count; j++)
                {
                    DocumentLine line = lines[j];
                    if (line != null && line.Id == selected)
                        return "의심  " + line.Sentence;
                }
            }

            return "의심  없음";
        }

        void BuildOverlay()
        {
            Font font = UiFont();
            var canvasObject = new GameObject("InterviewOverlay");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasObject.GetComponent<RectTransform>();
            var background = CreateImage(root, "Background", new Color(0.05f, 0.06f, 0.07f, 1f));
            Stretch(background.rectTransform, 0f);

            _themeLabel = CreateText(root, "Theme", 34, FontStyle.Bold, TextAnchor.MiddleLeft);
            Place(_themeLabel.rectTransform, 0.03f, 0.90f, 0.72f, 0.98f);

            _finishedLabel = CreateText(root, "Finished", 32, FontStyle.Bold, TextAnchor.MiddleRight);
            _finishedLabel.text = "면접 종료";
            _finishedLabel.color = new Color(1f, 0.84f, 0.35f, 1f);
            Place(_finishedLabel.rectTransform, 0.70f, 0.90f, 0.97f, 0.98f);
            _finishedLabel.gameObject.SetActive(false);

            var applicantPanel = CreateImage(root, "ApplicantPanel", new Color(0.10f, 0.12f, 0.14f, 1f));
            Place(applicantPanel.rectTransform, 0.02f, 0.20f, 0.26f, 0.88f);
            _applicantLabel = CreateText(applicantPanel.transform, "Applicant", 26, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(_applicantLabel.rectTransform, 16f);

            var documentPanel = CreateImage(root, "DocumentPanel", new Color(0.10f, 0.12f, 0.14f, 1f));
            Place(documentPanel.rectTransform, 0.28f, 0.20f, 0.70f, 0.88f);
            BuildDocumentButtons(documentPanel.transform, font);
            BuildLineButtons(documentPanel.transform, font);

            var teamPanel = CreateImage(root, "TeamPanel", new Color(0.10f, 0.12f, 0.14f, 1f));
            Place(teamPanel.rectTransform, 0.72f, 0.20f, 0.98f, 0.88f);
            BuildTeamReadout(teamPanel.transform);

            var suspicionPanel = CreateImage(root, "Suspicion", SuspicionColor);
            Place(suspicionPanel.rectTransform, 0.02f, 0.03f, 0.50f, 0.17f);
            _suspicionLabel = CreateText(suspicionPanel.transform, "SuspicionText", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
            _suspicionLabel.color = Color.black;
            Stretch(_suspicionLabel.rectTransform, 16f);

            _acceptButton = CreateButton(root, "Accept", "합격", AcceptColor, font, 32);
            Place(_acceptButton.GetComponent<RectTransform>(), 0.52f, 0.03f, 0.74f, 0.17f);
            _acceptButton.onClick.AddListener(OnAccept);

            _rejectButton = CreateButton(root, "Reject", "불합격", RejectColor, font, 32);
            Place(_rejectButton.GetComponent<RectTransform>(), 0.76f, 0.03f, 0.98f, 0.17f);
            _rejectButton.onClick.AddListener(OnReject);
        }

        void BuildDocumentButtons(Transform parent, Font font)
        {
            for (int i = 0; i < _documentButtons.Length; i++)
            {
                int index = i;
                float minX = 0.02f + i * 0.245f;
                Button button = CreateButton(parent, "Document" + i, string.Empty, ApplicationColor, font, 22);
                Place(button.GetComponent<RectTransform>(), minX, 0.80f, minX + 0.225f, 0.96f);
                button.onClick.AddListener(() => OnDocument(index));
                _documentButtons[i] = button;
                _documentLabels[i] = button.GetComponentInChildren<Text>();
            }
        }

        void BuildLineButtons(Transform parent, Font font)
        {
            for (int i = 0; i < LineSlotCount; i++)
            {
                int index = i;
                float maxY = 0.74f - i * 0.36f;
                Button button = CreateButton(parent, "Line" + i, string.Empty, SuspicionColor, font, 24);
                Place(button.GetComponent<RectTransform>(), 0.04f, maxY - 0.30f, 0.96f, maxY);
                button.onClick.AddListener(() => OnLine(index));
                _lineButtons[i] = button;
                _lineLabels[i] = button.GetComponentInChildren<Text>();
            }
        }

        void BuildTeamReadout(Transform parent)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                float maxY = 0.96f - i * 0.16f;
                Text slot = CreateText(parent, "Slot" + i, 20, FontStyle.Normal, TextAnchor.UpperLeft);
                Place(slot.rectTransform, 0.04f, maxY - 0.15f, 0.96f, maxY);
                _slotLabels[i] = slot;
            }

            _sumLabel = CreateText(parent, "DisplayedSum", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            Place(_sumLabel.rectTransform, 0.04f, 0.16f, 0.96f, 0.30f);
            _requiredLabel = CreateText(parent, "Required", 20, FontStyle.Normal, TextAnchor.MiddleLeft);
            Place(_requiredLabel.rectTransform, 0.04f, 0.02f, 0.96f, 0.15f);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            var module = eventObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        static string SkillLine(string title, int planning, int programming, int graphics)
        {
            return title
                + "  기획 " + planning
                + "  프로그래밍 " + programming
                + "  그래픽 " + graphics;
        }

        static string DocumentLabel(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Application:
                    return "지원서";
                case DocumentKind.Portfolio:
                    return "포트폴리오";
                case DocumentKind.Schedule:
                    return "일정표";
                case DocumentKind.InterviewRecord:
                    return "면접 기록";
                default:
                    return string.Empty;
            }
        }

        static Color DocumentColor(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Application:
                    return ApplicationColor;
                case DocumentKind.Portfolio:
                    return PortfolioColor;
                case DocumentKind.Schedule:
                    return ScheduleColor;
                case DocumentKind.InterviewRecord:
                    return InterviewRecordColor;
                default:
                    return Color.gray;
            }
        }

        static Button CreateButton(Transform parent, string name, string caption, Color color, Font font, int size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var image = buttonObject.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = Color.white;
            image.raycastTarget = true;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            Paint(button, color);
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Text label = CreateText(buttonObject.transform, "Label", size, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.font = font;
            label.text = caption;
            label.color = PaperColor;
            Stretch(label.rectTransform, 8f);
            return button;
        }

        static void Paint(Button button, Color color)
        {
            ColorBlock block = button.colors;
            block.normalColor = color;
            block.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
            block.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            block.selectedColor = color;
            block.disabledColor = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 0.65f);
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.05f;
            button.colors = block;
        }

        static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var text = textObject.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = PaperColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            return text;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var image = imageObject.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        static Font UiFont()
        {
            if (_font != null)
                return _font;

            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim" }, 40);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        static Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            Texture2D texture = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _whiteSprite;
        }
    }
}
