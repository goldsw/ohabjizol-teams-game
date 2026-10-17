using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>Serialized Start menu UI. App commands use the desktop's existing registry.</summary>
    public sealed class DesktopStartMenu : MonoBehaviour
    {
        [SerializeField] Button _start;
        [SerializeField] GameObject _menuRoot;
        [SerializeField] Button _code;
        [SerializeField] Button _settings;
        [SerializeField] Button _tutorial;
        DesktopView _desktop;
        DesktopIcon _codeApp;
        DesktopIcon _settingsApp;
        DesktopIcon _tutorialApp;

        public void Bind(DesktopView desktop, DesktopIcon code, DesktopIcon settings, DesktopIcon tutorial)
        {
            Unbind();
            _desktop = desktop;
            _codeApp = code;
            _settingsApp = settings;
            _tutorialApp = tutorial;
            if (isActiveAndEnabled) Connect();
        }
        public void Unbind()
        {
            Disconnect();
            Close();
            _desktop = null;
            _codeApp = _settingsApp = _tutorialApp = null;
        }
        public void Close() { if (_menuRoot != null) _menuRoot.SetActive(false); }
        void Toggle()
        {
            if (_desktop == null || !_desktop.CanActivateApps || _menuRoot == null) return;
            _menuRoot.SetActive(!_menuRoot.activeSelf);
        }
        void OpenCode() { Open(_codeApp); }
        void OpenSettings() { Open(_settingsApp); }
        void OpenTutorial() { Open(_tutorialApp); }
        void Open(DesktopIcon appId)
        {
            Close();
            if (_desktop != null) _desktop.ActivateApp(appId);
        }
        void Connect()
        {
            Disconnect();
            if (_desktop == null || _start == null || _menuRoot == null) return;
            _start.onClick.AddListener(Toggle);
            if (_code != null) _code.onClick.AddListener(OpenCode);
            if (_settings != null) _settings.onClick.AddListener(OpenSettings);
            if (_tutorial != null) _tutorial.onClick.AddListener(OpenTutorial);
        }
        void Disconnect()
        {
            if (_start != null) _start.onClick.RemoveListener(Toggle);
            if (_code != null) _code.onClick.RemoveListener(OpenCode);
            if (_settings != null) _settings.onClick.RemoveListener(OpenSettings);
            if (_tutorial != null) _tutorial.onClick.RemoveListener(OpenTutorial);
        }
        void OnEnable() { Close(); Connect(); }
        void OnDisable() { Disconnect(); Close(); }
        void OnDestroy() { Unbind(); }
#if UNITY_EDITOR
        public void ConfigureEditor(Button start, GameObject menuRoot, Button code, Button settings, Button tutorial)
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Start menu authoring requires Edit Mode.");
            Disconnect();
            _start = start;
            _menuRoot = menuRoot;
            _code = code;
            _settings = settings;
            _tutorial = tutorial;
            Close();
        }
#endif
    }
}
