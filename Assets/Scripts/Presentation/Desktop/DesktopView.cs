using System;
using System.Collections.Generic;
using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// The monitor desktop: wallpaper, a fixed icon column, the taskbar, and registered app windows.
    /// Launching 청강코드 hands the next stage to the menu controller, which owns the camera and scene path.
    /// </summary>
    public sealed partial class DesktopView : MonoBehaviour
    {
        static readonly Color WallpaperTop = new Color32(0x1B, 0x4F, 0x8F, 0xFF);
        static readonly Color WallpaperBottom = new Color32(0x08, 0x16, 0x33, 0xFF);
        static readonly Color TaskbarColor = new Color(0.07f, 0.08f, 0.10f, 0.94f);
        static readonly Color StartBlue = new Color32(0x4C, 0xC2, 0xFF, 0xFF);
        static readonly Color CodeTile = new Color32(0x58, 0x65, 0xF2, 0xFF);
        static readonly Color SettingsTile = new Color32(0x5C, 0x63, 0x70, 0xFF);
        static readonly Color TutorialTile = new Color32(0x23, 0xA5, 0x5A, 0xFF);

        [SerializeField] Image wallpaperImage;

        [SerializeField] List<DesktopIcon> _icons = new List<DesktopIcon>();
        [SerializeField] List<DesktopAppBinding> _appBindings = new List<DesktopAppBinding>();
        readonly Dictionary<DesktopIcon, DesktopAppBinding> _registry = new Dictionary<DesktopIcon, DesktopAppBinding>();
        readonly List<DesktopAppBinding> _subscribed = new List<DesktopAppBinding>();
        DesktopAppContext _appContext;
        bool _bound;
        DesktopLayout _layout;
        float _connectDelay;
        Action<StageId> _launch;
        SettingsApplier _settings;
        [SerializeField] ChungkangCodeWindow _code;
        [SerializeField] SettingsWindow _settingsWindow;
        [SerializeField] TutorialWindow _tutorial;
        [SerializeField] DesktopIcon _codeIcon;
        [SerializeField] DesktopIcon _settingsIcon;
        [SerializeField] DesktopIcon _tutorialIcon;
        [SerializeField] Text _stageLabel;
        [SerializeField] Text _clockLabel;
        [SerializeField] DesktopStartMenu _startMenu;
        string _clockShown;
        bool _locked;

        public void Lock() { _locked = true; if (_startMenu != null) _startMenu.Close(); }
        public TutorialWindow Tutorial => _tutorial;
        public void Unlock() { _locked = false; }
        public void ResetConnection() { if (_code != null) _code.ResetConnection(); }
        public void ShowTutorialPrompt(bool allowSkip)
        {
            if (_tutorial == null) return;
            ClearSelection();
            _tutorial.ConfigureMode(allowSkip);
            _tutorial.Open();
        }
        public void ClearSelection()
        {
            if (_startMenu != null) _startMenu.Close();
            foreach (var icon in _registry.Keys) if (icon != null) icon.Selected = false;
        }
        void Update() { UpdateClock(); }
        void Select(DesktopIcon icon)
        {
            if (_locked || !_registry.ContainsKey(icon)) return;
            foreach (var registered in _registry.Keys)
                if (registered != null) registered.Selected = registered == icon;
        }
        public bool CanActivateApps => !_locked && _bound && isActiveAndEnabled;
        void Activate(DesktopIcon icon) { ActivateApp(icon); }
        public bool ActivateApp(DesktopIcon appId)
        {
            if (!CanActivateApps || appId == null || !_registry.TryGetValue(appId, out var binding)) return false;
            if (_startMenu != null) _startMenu.Close();
            binding.Activate(_appContext);
            return true;
        }
        void Launch(StageId stage)
        {
            if (_locked || !isActiveAndEnabled) return;
            _launch?.Invoke(stage);
        }
        public IReadOnlyList<DesktopAppBinding> AppBindings
        {
            get
            {
                EnsureAppBindings();
                return _appBindings.AsReadOnly();
            }
        }
        public bool HasAuthoredUi
        {
            get
            {
                EnsureAppBindings();
                return wallpaperImage != null && _stageLabel != null && _clockLabel != null &&
                    ValidateBindings(false);
            }
        }
        public bool Bind(float connectDelay, Action<StageId> launch)
        {
            DisconnectApps();
            _bound = false;
            _registry.Clear();
            _connectDelay = connectDelay;
            _launch = launch;
            if (!HasAuthoredUi)
            {
                ValidateBindings(true);
                Debug.LogError("Desktop authored references incomplete. Capture and save in Edit Mode.", this);
                return false;
            }
            foreach (var binding in _appBindings)
            {
                binding.Window.InitializeAuthored();
                if (binding.Window.Root == null || binding.Window.Toolbar == null || binding.Window.Info == null)
                {
                    Debug.LogError("Desktop window frame missing. Capture and save the prefab.", binding.Window);
                    return false;
                }
                if (binding.Icon.TryGetComponent<Button>(out var appButton))
                    ButtonActiveBackground.Ensure(appButton).TrackVisibleContent(binding.Window.Info.gameObject);
            }
            if (_settings == null) _settings = SettingsApplier.LoadAndApply();
            _appContext = new DesktopAppContext(_settings, () => _connectDelay, Launch, RefreshAuthoredContent);
            foreach (var binding in _appBindings) _registry.Add(binding.Icon, binding);
            _bound = true;
            if (isActiveAndEnabled) ConnectApps();
            var background = wallpaperImage.GetComponent<DesktopBackground>();
            if (background != null) background.Attach(this);
            RefreshAuthoredContent();
            return true;
        }
        bool ValidateBindings(bool diagnose)
        {
            if (_appBindings == null || _appBindings.Count == 0) return false;
            var icons = new HashSet<DesktopIcon>();
            var windows = new HashSet<InfoWindow>();
            bool valid = true;
            for (int i = 0; i < _appBindings.Count; i++)
            {
                var binding = _appBindings[i];
                bool itemValid = binding != null && binding.HasAuthoredContent;
                if (binding != null)
                {
                    if (binding.Icon != null && !icons.Add(binding.Icon)) itemValid = false;
                    if (binding.Window != null && !windows.Add(binding.Window)) itemValid = false;
                }
                if (itemValid) continue;
                valid = false;
                if (diagnose) Debug.LogError("Desktop app binding " + i +
                    ": invalid icon/window/content/handler or duplicate icon/window.", this);
            }
            return valid;
        }
        void ConnectApps()
        {
            DisconnectApps();
            foreach (var binding in _registry.Values)
            {
                binding.Icon.Clicked += Select;
                binding.Icon.Activated += Activate;
                binding.Attach(_appContext);
                _subscribed.Add(binding);
            }
            if (_startMenu != null) _startMenu.Bind(this, _codeIcon, _settingsIcon, _tutorialIcon);
        }
        void DisconnectApps()
        {
            if (_startMenu != null) _startMenu.Unbind();
            foreach (var binding in _subscribed)
            {
                if (binding.Icon != null)
                {
                    binding.Icon.Clicked -= Select;
                    binding.Icon.Activated -= Activate;
                }
                binding.Detach(_appContext);
            }
            _subscribed.Clear();
        }
        void OnEnable()
        {
            if (!_bound) return;
            ConnectApps();
            RefreshAuthoredContent();
        }
        void OnDisable() { DisconnectApps(); }
        void OnDestroy() { DisconnectApps(); }
        public void RefreshAuthoredContent()
        {
            EnsureAppBindings();
            if (Application.isPlaying) RefreshStage();
            foreach (var binding in _appBindings)
                if (binding != null && binding.HasAuthoredContent) binding.Window.Refresh();
            UpdateClock();
        }
        void UpdateClock()
        {
            if (_clockLabel == null)
                return;

            string now = DateTime.Now.ToString("HH:mm");
            if (now == _clockShown)
                return;

            _clockShown = now;
            _clockLabel.text = now;
        }
        void RefreshStage()
        {
            var run = CampaignRunStore.Load();
            StageId next = run.NextStage;
            if (_stageLabel != null)
                _stageLabel.text = run.IsComplete ? "면접 완료 · 엔딩" : StageCatalog.Get(next).DisplayName;
        }
    }
    /// <summary>Empty-desktop click clears the icon selection.</summary>
    sealed class DesktopBackground : MonoBehaviour, IPointerClickHandler
    {
        DesktopView _view;

        public void Attach(DesktopView view)
        {
            _view = view;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_view != null)
                _view.ClearSelection();
        }
    }
}
