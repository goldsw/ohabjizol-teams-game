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

        public RadialQuestionWindow Window => _window;

        public void ConfigureEditor(Button button, RadialQuestionWindow window)
        {
            if (_button) _button.onClick.RemoveListener(Invoke);
            _button = button;
            _window = window;
            if (isActiveAndEnabled && _button) _button.onClick.AddListener(Invoke);
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
            if (!_window) return;
            _window.InitializeAuthored();
            if (!_window.IsOpen || _window.IsMinimized)
            {
                _window.Restore();
                return;
            }
            // WindowToolbar brings windows forward by sibling order.
            Transform root = _window.transform;
            if (root.parent)
            {
                for (int i = root.GetSiblingIndex() + 1; i < root.parent.childCount; i++)
                {
                    Transform sibling = root.parent.GetChild(i);
                    var other = sibling.GetComponent<WindowToolbar>();
                    if (other && other.IsOpen)
                    {
                        _window.BringToFront();
                        return;
                    }
                }
            }
            _window.Minimize();
        }
    }
}
