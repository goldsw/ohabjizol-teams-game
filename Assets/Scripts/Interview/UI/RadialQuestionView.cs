using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class RadialQuestionView : MonoBehaviour
    {
        [SerializeField] Button _launcher;
        [SerializeField] RectTransform _optionsRoot;
        [SerializeField] RectTransform _portrait;
        [SerializeField] Button[] _questions = new Button[5];
        [SerializeField] float _startAngle = 90f;
        [SerializeField, Min(0f)] float _radius = 180f;
        [SerializeField] float _rotationDegreesPerSecond = 100f;
        [SerializeField] RadialQuestionWindow _window;
        [SerializeField] RadialQuestionLauncher _fixedLauncher;
        float _phase;
        bool _expanded;
        bool _available;

        public bool IsExpanded => _expanded;

        public void ConfigureEditor(Button launcher, RectTransform optionsRoot,
            RectTransform portrait, Button[] questions)
        {
            if (isActiveAndEnabled && _launcher) _launcher.onClick.RemoveListener(ToggleExpanded);
            _launcher = launcher;
            _optionsRoot = optionsRoot;
            _portrait = portrait;
            _questions = questions == null ? new Button[0] : (Button[])questions.Clone();
            if (isActiveAndEnabled && _launcher) _launcher.onClick.AddListener(ToggleExpanded);
            ResetCollapsed();
            SetAvailable(false);
        }

        public void ConfigureWindowEditor(RadialQuestionWindow window, RadialQuestionLauncher fixedLauncher)
        {
            _window = window;
            _fixedLauncher = fixedLauncher;
        }

        void OnEnable()
        {
            if (_launcher)
            {
                _launcher.onClick.RemoveListener(ToggleExpanded);
                _launcher.onClick.AddListener(ToggleExpanded);
            }
        }

        void OnDisable()
        {
            if (_launcher) _launcher.onClick.RemoveListener(ToggleExpanded);
            SetAvailable(false);
            ResetCollapsed();
        }

        public void ResetCollapsed()
        {
            _phase = Mathf.Repeat(_startAngle, 360f);
            CollapseContent();
        }

        public void CollapseContent()
        {
            _expanded = false;
            if (_optionsRoot) _optionsRoot.gameObject.SetActive(false);
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            if (_launcher) _launcher.interactable = available;
        }

        public void ShowQuestions()
        {
            if (!_available || !_optionsRoot || !_portrait) return;
            _expanded = true;
            PositionQuestions();
            _optionsRoot.gameObject.SetActive(true);
        }

        public void ToggleExpanded()
        {
            if (!_available || !_optionsRoot || !_portrait) return;
            _expanded = !_expanded;
            if (_expanded) PositionQuestions();
            _optionsRoot.gameObject.SetActive(_expanded);
        }

        void Update()
        {
            if (!_expanded || !_optionsRoot || !_portrait) return;
            _phase = Mathf.Repeat(_phase - Mathf.Abs(_rotationDegreesPerSecond) * Time.unscaledDeltaTime, 360f);
            PositionQuestions();
        }

        void PositionQuestions()
        {
            if (_questions == null || _questions.Length == 0) return;
            Vector3 worldCenter = _portrait.TransformPoint(_portrait.rect.center);
            Vector3 center = _optionsRoot.InverseTransformPoint(worldCenter);
            float step = 360f / _questions.Length;
            for (int i = 0; i < _questions.Length; i++)
            {
                if (!_questions[i]) continue;
                var rect = _questions[i].transform as RectTransform;
                if (!rect) continue;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                float angle = (_phase - step * i) * Mathf.Deg2Rad;
                rect.localPosition = new Vector3(center.x + Mathf.Cos(angle) * _radius,
                    center.y + Mathf.Sin(angle) * _radius, center.z);
                rect.localRotation = Quaternion.identity;
            }
        }
    }
}
