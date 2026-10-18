using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [DisallowMultipleComponent]
    public sealed class InterviewTeamLauncher : MonoBehaviour
    {
        [SerializeField] InterviewTeamWindow _window;
        [SerializeField] Button _button;
        [SerializeField] Image _surface;
        [SerializeField] Color _normal = new Color32(255, 174, 76, 255);
        [SerializeField] Color _minimized = new Color32(105, 105, 105, 255);
        bool _started;

        void OnEnable()
        {
            if (_button) { _button.onClick.RemoveListener(Toggle); _button.onClick.AddListener(Toggle); }
            RefreshColor();
        }
        void Start()
        {
            if (_started || !_window) return;
            _started = true;
            _window.ApplyInitialState(true);
            RefreshColor();
        }
        void OnDisable() { if (_button) _button.onClick.RemoveListener(Toggle); }
        void LateUpdate() { RefreshColor(); }
        void Toggle()
        {
            if (!_window) return;
            _window.InitializeAuthored();
            if (_window.IsOpen) _window.Minimize();
            else _window.Restore();
            RefreshColor();
        }
        void RefreshColor()
        {
            if (!_surface || !_window) return;
            Color color = _window.IsMinimized ? _minimized : _normal;
            if (_surface.color != color) _surface.color = color;
        }
#if UNITY_EDITOR
        public void ConfigureEditor(InterviewTeamWindow window, Button button, Image surface)
        {
            _window = window; _button = button; _surface = surface;
        }
#endif
    }
}
