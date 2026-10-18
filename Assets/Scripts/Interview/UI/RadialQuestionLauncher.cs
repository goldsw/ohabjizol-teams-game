using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [DisallowMultipleComponent]
    public sealed class RadialQuestionLauncher : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] RadialQuestionWindow _window;
        [SerializeField] RadialQuestionView _questions;
        [SerializeField] Color _collapsedColor = new Color32(240, 158, 74, 255);
        [SerializeField] Color _expandedColor = new Color32(255, 206, 128, 255);

        public RadialQuestionWindow Window => _window;

        public void ConfigureEditor(Button button, RadialQuestionWindow window)
        {
            if (_button) _button.onClick.RemoveListener(Invoke);
            _button = button;
            _window = window;
            if (isActiveAndEnabled && _button) _button.onClick.AddListener(Invoke);
        }

        public void ConfigureEditor(Button button, RadialQuestionWindow window, RadialQuestionView questions)
        {
            ConfigureEditor(button, window);
            _questions = questions;
            RefreshColor();
        }

        void LateUpdate()
        {
            RefreshColor();
        }

        void RefreshColor()
        {
            if (_button && _button.image)
                _button.image.color = _questions && _questions.IsExpanded ? _expandedColor : _collapsedColor;
        }

        void OnEnable()
        {
            if (!_button) return;
            _button.interactable = true;
            _button.onClick.RemoveListener(Invoke);
            _button.onClick.AddListener(Invoke);
        }

        void OnDisable()
        {
            if (_button) _button.onClick.RemoveListener(Invoke);
        }

        public void Invoke()
        {
            if (_questions) _questions.ToggleExpanded();
            RefreshColor();
        }
    }
}
