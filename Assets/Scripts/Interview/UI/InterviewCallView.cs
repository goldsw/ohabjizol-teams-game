using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed class InterviewCallView : MonoBehaviour
    {
        [SerializeField] Image _portrait;
        [SerializeField] Text _name;
        [SerializeField] Text _dots;
        [SerializeField] Image speechBubble;
        [SerializeField, Min(.01f)] float applicantFadeDuration = .25f;
        bool _talking;
        bool _arriving, _departing;
        float _opacity = 1f, _departureElapsed, _departureHold;
        float _bubbleAlpha = .98f, _dotsAlpha = 1f, _nameAlpha = 1f;
        bool _alphasCaptured;
        float _bubbleWidth;
        [SerializeField] InterviewTeamSlotView _participant;
        [SerializeField] Text _participantCaption;
        [SerializeField] Image interviewerProfilePortrait;
#if UNITY_EDITOR
        public void ConfigureInterviewerProfileEditor(Image portrait)
        {
            interviewerProfilePortrait = portrait;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        public void ConfigureParticipantEditor(InterviewTeamSlotView participant, Text caption)
        { _participant = participant; _participantCaption = caption; }
#endif
        [SerializeField] ApplicantVisualCatalog _visualCatalog;
        public ApplicantVisualCatalog VisualCatalog => _visualCatalog;
        public RectTransform PortraitRect => _portrait ? _portrait.rectTransform : null;
        public bool HasAuthoredUi => _portrait && _name && _dots;
        public void Configure(Image portrait, Text name, Text dots) { _portrait = portrait; _name = name; _dots = dots; }
        public void ConfigureSpeechBubbleEditor(Image bubble, Text dots)
        {
            speechBubble = bubble;
            _dots = dots;
            SetAnswering(false, 0);
        }
        public void Bind(Applicant applicant)
        {
            CaptureBaseAlphas();
            _departing = false;
            _arriving = applicant != null;
            _opacity = 0f;
            if (speechBubble && _bubbleWidth > 0f)
                speechBubble.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _bubbleWidth);
            SetAnswering(false, 0);
            if (_name) _name.text = applicant == null ? "" : applicant.Name + " · " + JobRoleText.Korean(applicant.PrimaryRole);
            if (_portrait) _portrait.enabled = applicant != null;
            if (_participant)
            {
                _participant.BindCharacterPortrait(applicant == null ? null : applicant.CharacterId, _visualCatalog);
                _participant.gameObject.SetActive(applicant != null);
            }
            if (_participantCaption) _participantCaption.text = applicant == null ? "" : applicant.Name + " · " + JobRoleText.Korean(applicant.PrimaryRole);
            if (interviewerProfilePortrait)
            {
                interviewerProfilePortrait.sprite = applicant != null && _visualCatalog && _visualCatalog.TryGet(applicant.CharacterId, out var visual)
                    ? visual.Idle : null;
                interviewerProfilePortrait.enabled = interviewerProfilePortrait.sprite != null;
            }
            ApplyOpacity();
        }

        void CaptureBaseAlphas()
        {
            if (_alphasCaptured) return;
            _alphasCaptured = true;
            if (speechBubble)
            {
                _bubbleAlpha = speechBubble.color.a;
                _bubbleWidth = speechBubble.rectTransform.rect.width;
            }
            if (_dots) _dotsAlpha = _dots.color.a;
            if (_name) _nameAlpha = _name.color.a;
        }

        public float BeginDeparture(string message, float holdSeconds)
        {
            CaptureBaseAlphas();
            _arriving = false;
            _departing = true;
            _departureElapsed = 0f;
            _departureHold = Mathf.Max(0f, holdSeconds);
            _opacity = 1f;
            SetThinking(message);
            if (speechBubble && _dots && !string.IsNullOrEmpty(message))
                speechBubble.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(_bubbleWidth, _dots.preferredWidth + 16f));
            SetTalking(!string.IsNullOrEmpty(message));
            ApplyOpacity();
            return _departureHold + Mathf.Max(.01f, applicantFadeDuration);
        }

        public void CancelDeparture()
        {
            if (!_departing) return;
            _departing = false;
            _opacity = 1f;
            SetAnswering(false, 0);
            ApplyOpacity();
        }

        void Update()
        {
            float duration = Mathf.Max(.01f, applicantFadeDuration);
            if (_departing)
            {
                _departureElapsed += Time.unscaledDeltaTime;
                _opacity = 1f - Mathf.Clamp01((_departureElapsed - _departureHold) / duration);
            }
            else if (_arriving)
            {
                _opacity = Mathf.MoveTowards(_opacity, 1f, Time.unscaledDeltaTime / duration);
                if (_opacity >= 1f) _arriving = false;
            }
            else return;
            ApplyOpacity();
        }

        void ApplyOpacity()
        {
            SetAlpha(_portrait, _opacity);
            SetAlpha(_name, _nameAlpha * _opacity);
            SetAlpha(speechBubble, _bubbleAlpha * _opacity);
            SetAlpha(_dots, _dotsAlpha * _opacity);
        }

        static void SetAlpha(Graphic graphic, float alpha)
        {
            if (!graphic) return;
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
        public void SetThinking(string text)
        {
            if (_dots) _dots.text = text ?? "";
            RefreshSpeechBubble();
        }
        public void SetTalking(bool talking)
        {
            _talking = talking;
            RefreshSpeechBubble();
        }
        void RefreshSpeechBubble()
        {
            bool visible = _talking && _dots && !string.IsNullOrEmpty(_dots.text);
            if (speechBubble) speechBubble.gameObject.SetActive(visible);
            if (_dots) _dots.enabled = visible;
        }
        public void SetAnswering(bool answering, int tick)
        {
            if (_departing) return;
            SetTalking(answering);
            SetThinking(answering ? new string('.', tick % 3 + 1) : "");
        }
        public void SetPose(Sprite sprite)
        {
            if (!_portrait) return;
            _portrait.sprite = sprite;
            _portrait.color = _portrait.sprite ? Color.white : new Color(.21f, .26f, .34f);
            ApplyOpacity();
        }
        void OnDisable() { if (!_departing) SetAnswering(false, 0); }
    }
}
