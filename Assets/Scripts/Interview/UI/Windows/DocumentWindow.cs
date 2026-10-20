using System;
using System.Collections.Generic;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// One document kind. <see cref="Bind"/> then <see cref="Refresh"/> draws <see cref="InterviewSession.Current"/>.
    /// Line clicks call <see cref="InterviewSession.SelectLine"/> and then the bound callback.
    /// </summary>
    public sealed class DocumentWindow : InfoWindow
    {
        public const float DefaultWidth = 560f;
        public const float DefaultHeight = 700f;

        const int LineSlotCount = 2;
        const float BandHeight = 10f;
        const float HeaderTop = 22f;
        const float HeaderHeight = 180f;
        const float LineGap = 12f;
        const float LineInset = 16f;

        static readonly Color Ink = new Color(0.12f, 0.14f, 0.16f, 1f);
        static readonly Color NameField = new Color(0.96f, 0.97f, 0.98f, 1f);
        static readonly Color SuspicionColor = new Color(0.75f, 0.62f, 0.16f, 1f);
        static readonly Color SuspicionSelectedColor = new Color(0.98f, 0.86f, 0.22f, 1f);

        InterviewSession _session;
        Action _changed;
        DocumentKind _kind;
        [SerializeField] Image _band;
        [SerializeField] GameObject _header;
        [SerializeField] Text _name;
        [SerializeField] Text _role;
        [SerializeField] Text _skills;
        [SerializeField] ApplicantStatsView _applicantStats;
        [SerializeField] Text _missing;
        [SerializeField] Button[] _lineButtons = new Button[LineSlotCount];
        [SerializeField] Text[] _lineLabels = new Text[LineSlotCount];
        readonly string[] _lineIds = new string[LineSlotCount];
        readonly UnityEngine.Events.UnityAction[] _lineActions = new UnityEngine.Events.UnityAction[LineSlotCount];

        public DocumentKind Kind
        {
            get { return _kind; }
        }

        public void Bind(InterviewSession session, DocumentKind kind, Action changed)
        {
            _session = session;
            _kind = kind;
            _changed = changed;
            Title = DocumentKindText.Korean(kind);
            Refresh();
        }

        protected override void OnAuthoredInitialized() { BindLineButtons(); }

        void BindLineButtons()
        {
            for (int i = 0; i < LineSlotCount; i++)
            {
                int index = i;
                if (_lineButtons[i] == null) continue;
                if (_lineActions[i] == null) _lineActions[i] = () => OnLine(index);
                _lineButtons[i].onClick.RemoveListener(_lineActions[i]);
                _lineButtons[i].onClick.AddListener(_lineActions[i]);
            }
        }

        protected override void BuildInfo(RectTransform info)
        {
            Image band = UiKit.Image(info, "Band", DocumentKindText.Color(_kind));
            band.raycastTarget = false;
            _band = band;
            PinTop(band.rectTransform, 0f, BandHeight, 0f, 0f);

            _header = new GameObject("Header");
            _header.transform.SetParent(info, false);
            var headerRect = _header.AddComponent<RectTransform>();
            headerRect.localScale = Vector3.one;
            PinTop(headerRect, HeaderTop, HeaderHeight, LineInset, LineInset);

            Text nameCaption = UiKit.Text(_header.transform, "NameCaption", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            nameCaption.text = "이름";
            nameCaption.color = Ink;
            PinTop(nameCaption.rectTransform, 0f, 28f, 0f, 0f);

            Image nameBox = UiKit.Image(_header.transform, "NameBox", NameField);
            nameBox.raycastTarget = false;
            PinTop(nameBox.rectTransform, 32f, 48f, 0f, 0f);
            _name = UiKit.Text(nameBox.transform, "Name", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            _name.color = Ink;
            UiKit.Stretch(_name.rectTransform, 12f);

            _role = UiKit.Text(_header.transform, "Role", 20, FontStyle.Normal, TextAnchor.MiddleLeft);
            _role.color = Ink;
            PinTop(_role.rectTransform, 92f, 32f, 0f, 0f);

            _skills = UiKit.Text(_header.transform, "Skills", 18, FontStyle.Normal, TextAnchor.MiddleLeft);
            _skills.color = Ink;
            _skills.horizontalOverflow = HorizontalWrapMode.Wrap;
            PinTop(_skills.rectTransform, 124f, 52f, 0f, 0f);

            _missing = UiKit.Text(info, "Missing", 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            _missing.color = Ink;
            _missing.text = "제출되지 않음";
            UiKit.Stretch(_missing.rectTransform, 24f);

            for (int i = 0; i < LineSlotCount; i++)
            {
                int index = i;
                Button button = UiKit.Button(info, "Line" + i, string.Empty, SuspicionColor, 24);
                Text label = button.GetComponentInChildren<Text>();
                label.color = Color.black;
                label.alignment = TextAnchor.MiddleLeft;

                _lineButtons[i] = button;
                _lineLabels[i] = label;
            }

            Refresh();
        }

        public override void Refresh()
        {
            if (_missing == null)
                return;

            if (_band != null)
                _band.color = DocumentKindText.Color(_kind);

            Applicant current = _session == null ? null : _session.Current;
            bool showHeader = _kind == DocumentKind.Application && current != null;
            if (_header != null)
                _header.SetActive(showHeader);
            WriteHeader(showHeader ? current : null);

            ApplicantDocument document = FindDocument(current, _kind);
            bool interactive = _session != null && !_session.IsFinished && current != null && document != null;
            _missing.gameObject.SetActive(document == null);
            WriteLines(document, interactive);
            if (!Application.isPlaying && Application.isEditor) LayoutBody(showHeader);
        }

        void WriteHeader(Applicant current)
        {
            if (_applicantStats)
                _applicantStats.Bind(current == null ? null : current.DisplayedSkills);
            if (_skills) _skills.enabled = !_applicantStats;
            if (_name == null)
                return;

            if (current == null)
            {
                _name.text = string.Empty;
                _role.text = string.Empty;
                _skills.text = string.Empty;
                return;
            }

            _name.text = current.Name;
            _role.text = "주 직군  " + JobRoleText.Korean(current.PrimaryRole);
            if (_applicantStats) { _skills.text = string.Empty; return; }
            SkillSet shown = current.DisplayedSkills;
            if (shown == null)
                _skills.text = "예상능력";
            else
                _skills.text = "예상능력  기획 " + shown.Planning
                    + "  프로그래밍 " + shown.Programming
                    + "  그래픽 " + shown.Graphics;
        }

#if UNITY_EDITOR
        // Legacy windows can opt into an explicitly authored view without rebuilding their header.
        public void ConfigureApplicantStatsEditor(ApplicantStatsView stats)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stats authoring requires EditMode.");
            if (!stats || !stats.HasAuthoredUi || !stats.transform.IsChildOf(transform))
                throw new ArgumentException("An authored stats view inside this window is required.", nameof(stats));
            _applicantStats = stats;
            Refresh();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        void WriteLines(ApplicantDocument document, bool interactive)
        {
            for (int i = 0; i < LineSlotCount; i++)
            {
                _lineIds[i] = null;
                _lineButtons[i].gameObject.SetActive(false);
            }

            if (document == null || document.Lines == null)
                return;

            IReadOnlyList<DocumentLine> lines = document.Lines;
            int count = lines.Count < LineSlotCount ? lines.Count : LineSlotCount;
            string selected = _session == null ? null : _session.SelectedLineId;
            for (int i = 0; i < count; i++)
            {
                DocumentLine line = lines[i];
                if (line == null)
                    continue;

                bool chosen = !string.IsNullOrEmpty(selected) && line.Id == selected;
                _lineIds[i] = line.Id;
                _lineLabels[i].text = chosen ? "의심  " + line.Sentence : line.Sentence;
                _lineLabels[i].color = Color.black;
                ButtonActiveBackground.Ensure(_lineButtons[i]).SetActiveState(chosen);
                _lineButtons[i].interactable = interactive;
                _lineButtons[i].gameObject.SetActive(true);
            }
        }

        void LayoutBody(bool showHeader)
        {
            float top = showHeader ? HeaderTop + HeaderHeight + 12f : BandHeight + 12f;
            float infoHeight = Info != null ? Info.rect.height : 0f;
            if (infoHeight < 1f && Root != null)
                infoHeight = Mathf.Max(0f, Root.sizeDelta.y - ToolbarHeight);
            float available = infoHeight - top - LineInset;
            if (available < 80f)
                available = 80f;

            float buttonHeight = (available - LineGap) * 0.5f;
            PinTop(_lineButtons[0].GetComponent<RectTransform>(), top, buttonHeight, LineInset, LineInset);
            PinTop(
                _lineButtons[1].GetComponent<RectTransform>(),
                top + buttonHeight + LineGap,
                buttonHeight,
                LineInset,
                LineInset);

            if (_missing != null)
            {
                float missingTop = showHeader ? HeaderTop + HeaderHeight : BandHeight;
                PinTop(_missing.rectTransform, missingTop, Mathf.Max(48f, infoHeight - missingTop), LineInset, LineInset);
            }
        }

        void OnLine(int index)
        {
            if (_session == null || index < 0 || index >= _lineIds.Length)
                return;
            if (_session.IsFinished || _session.Current == null)
                return;

            string lineId = _lineIds[index];
            if (string.IsNullOrEmpty(lineId))
                return;

            _session.SelectLine(lineId);
            Refresh();
            if (_changed != null)
                _changed();
        }

        static ApplicantDocument FindDocument(Applicant applicant, DocumentKind kind)
        {
            if (applicant == null || applicant.Documents == null)
                return null;

            IReadOnlyList<ApplicantDocument> documents = applicant.Documents;
            for (int i = 0; i < documents.Count; i++)
            {
                ApplicantDocument document = documents[i];
                if (document != null && document.Kind == kind)
                    return document;
            }

            return null;
        }

        static void PinTop(RectTransform rect, float top, float height, float left, float right)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }
    }
}
