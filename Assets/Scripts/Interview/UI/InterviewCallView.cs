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
        bool _talking;
        [Header("Artist sprites; assign persistent assets")]
        [SerializeField] Sprite _idleSprite;
        [SerializeField] Sprite _talkSprite;
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
            SetAnswering(false, 0);
            _name.text = applicant == null ? "" : applicant.Name + " · " + JobRoleText.Korean(applicant.PrimaryRole);
            _portrait.enabled = applicant != null;
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
            SetTalking(answering);
            SetThinking(answering ? new string('.', tick % 3 + 1) : "");
            if (!_portrait) return;
            _portrait.sprite = answering && (tick % 2) != 0 && _talkSprite ? _talkSprite : _idleSprite;
            _portrait.color = _portrait.sprite ? Color.white : new Color(.21f, .26f, .34f);
        }
        void OnDisable() { SetAnswering(false, 0); }
    }
}