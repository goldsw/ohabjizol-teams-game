using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// One call tile. Idle and talk frames swap once a second on unscaled time.
    /// A hidden tile draws nothing, so the backdrop avatar stays visible.
    /// </summary>
    public sealed class ParticipantPortrait : MonoBehaviour
    {
        const float FrameSeconds = 1f;
        const float NameplateHeight = 36f;

        Image _cover;
        Image _portrait;
        Image _nameplate;
        Text _nameLabel;
        Sprite[] _frames;
        Applicant _applicant;
        float _elapsed;
        int _frameIndex;
        bool _visible;
        bool _coverWhenShown;

        public void Build(bool preserveAspect, bool coverWhenShown, int nameSize)
        {
            _coverWhenShown = coverWhenShown;
            if (coverWhenShown)
            {
                _cover = UiKit.Image(transform, "Cover", new Color(0.05f, 0.05f, 0.06f, 1f));
                _cover.raycastTarget = false;
                UiKit.Stretch(_cover.rectTransform, 0f);
            }

            _portrait = UiKit.Image(transform, "Portrait", Color.white);
            _portrait.preserveAspect = preserveAspect;
            _portrait.raycastTarget = false;
            RectTransform face = _portrait.rectTransform;
            face.anchorMin = Vector2.zero;
            face.anchorMax = Vector2.one;
            face.offsetMin = new Vector2(6f, NameplateHeight);
            face.offsetMax = new Vector2(-6f, -6f);

            _nameplate = UiKit.Image(transform, "Nameplate", new Color(0f, 0f, 0f, 0.62f));
            _nameplate.raycastTarget = false;
            RectTransform plate = _nameplate.rectTransform;
            plate.anchorMin = new Vector2(0f, 0f);
            plate.anchorMax = new Vector2(1f, 0f);
            plate.pivot = new Vector2(0.5f, 0f);
            plate.anchoredPosition = Vector2.zero;
            plate.sizeDelta = new Vector2(0f, NameplateHeight);

            _nameLabel = UiKit.Text(_nameplate.transform, "Name", nameSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            _nameLabel.color = Color.white;
            UiKit.Stretch(_nameLabel.rectTransform, 4f);
            Hide();
        }

        public void Show(Applicant applicant, Sprite[] frames)
        {
            if (applicant == null || _portrait == null)
            {
                Hide();
                return;
            }

            bool reset = !ReferenceEquals(applicant, _applicant);
            _applicant = applicant;
            _frames = frames;
            _visible = true;

            if (_cover != null)
                _cover.gameObject.SetActive(_coverWhenShown);

            bool hasFrame = frames != null && frames.Length > 0 && frames[0] != null;
            _portrait.gameObject.SetActive(hasFrame);
            _nameplate.gameObject.SetActive(true);
            _nameLabel.text = applicant.Name + " · " + JobRoleText.Korean(applicant.PrimaryRole);

            if (!hasFrame)
                return;

            if (reset)
            {
                _elapsed = 0f;
                _frameIndex = 0;
            }

            if (_frameIndex < 0 || _frameIndex >= frames.Length)
                _frameIndex = 0;
            _portrait.sprite = frames[_frameIndex];
        }

        public void Hide()
        {
            _visible = false;
            _applicant = null;
            _elapsed = 0f;
            _frameIndex = 0;
            if (_cover != null)
                _cover.gameObject.SetActive(false);
            if (_portrait != null)
                _portrait.gameObject.SetActive(false);
            if (_nameplate != null)
                _nameplate.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_visible || _portrait == null || _frames == null || _frames.Length == 0)
                return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < FrameSeconds)
                return;

            _elapsed -= FrameSeconds;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            Sprite frame = _frames[_frameIndex];
            if (frame != null)
                _portrait.sprite = frame;
        }
    }
}
