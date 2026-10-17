using System;
using System.Collections.Generic;
using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Presentation
{
    public sealed partial class DesktopView
    {
        public void Build(RectTransform root, DesktopLayout layout, float connectDelay, Action<StageId> launch, DesktopInitialState initialWindows = null)
        {
            _layout = layout ?? new DesktopLayout();
            _connectDelay = connectDelay;
            _launch = launch;
            if (!Application.isEditor || Application.isPlaying) throw new InvalidOperationException("Desktop Build requires Edit Mode.");

            Image wallpaper = UiKit.Image(root, "Wallpaper", Color.white);
            wallpaperImage = wallpaper;
            wallpaper.color = new Color32(0xEC, 0x80, 0x32, 0xFF);
            wallpaper.raycastTarget = true;
            UiKit.Stretch(wallpaper.rectTransform, 0f);
            wallpaper.gameObject.AddComponent<DesktopBackground>().Attach(this);

            var iconsObject = new GameObject("Icons", typeof(RectTransform));
            iconsObject.transform.SetParent(root, false);
            RectTransform icons = iconsObject.GetComponent<RectTransform>();
            UiKit.Stretch(icons, 0f);

            _codeIcon = AddIcon(icons, "ChungkangCodeIcon", "청강코드", "청", CodeTile);
            _settingsIcon = AddIcon(icons, "SettingsIcon", "설정", "설", SettingsTile);
            _tutorialIcon = AddIcon(icons, "TutorialIcon", "튜토리얼", "튜", TutorialTile);

            var windowsObject = new GameObject("Windows", typeof(RectTransform));
            windowsObject.transform.SetParent(root, false);
            RectTransform windows = windowsObject.GetComponent<RectTransform>();
            UiKit.Stretch(windows, 0f);
            windows.offsetMin = new Vector2(0f, _layout.TaskbarHeight);

            _code = WindowFactory.Create<ChungkangCodeWindow>(windows, "청강코드", _layout.CodeWindowSize, _layout.CodeWindowPosition);
            _settingsWindow = WindowFactory.Create<SettingsWindow>(windows, "설정", _layout.SettingsWindowSize, _layout.SettingsWindowPosition);

            _tutorial = WindowFactory.Create<TutorialWindow>(windows, "튜토리얼", _layout.TutorialWindowSize, _layout.TutorialWindowPosition);

            EnsureAppBindings();
            BuildTaskbar(root);
#if UNITY_EDITOR
            AuthorStartMenu();
#endif
            if (Application.isPlaying) RefreshStage();
            UpdateClock();

            DesktopInitialState state = initialWindows ?? new DesktopInitialState();
            _appBindings[0].InitiallyOpen = state.codeOpen;
            _appBindings[1].InitiallyOpen = state.settingsOpen;
            _appBindings[2].InitiallyOpen = state.tutorialOpen;
            foreach (var binding in _appBindings) binding.Window.ApplyInitialState(binding.InitiallyOpen);
        }
        DesktopIcon AddIcon(RectTransform parent, string name, string label, string glyph, Color color)
        {
            DesktopIcon icon = DesktopIcon.Create(parent, name, label, glyph, color, _layout, _icons.Count);
            _icons.Add(icon);
            return icon;
        }
        void BuildTaskbar(RectTransform root)
        {
            Image bar = UiKit.Image(root, "Taskbar", TaskbarColor);
            bar.raycastTarget = true;
            RectTransform barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(0f, _layout.TaskbarHeight);
            barRect.anchoredPosition = Vector2.zero;

            float height = _layout.TaskbarHeight;
            Image start = UiKit.Image(barRect, "Start", new Color(1f, 1f, 1f, 0.06f));
            RectTransform startRect = start.rectTransform;
            startRect.anchorMin = new Vector2(0f, 0.5f);
            startRect.anchorMax = new Vector2(0f, 0.5f);
            startRect.pivot = new Vector2(0f, 0.5f);
            startRect.sizeDelta = new Vector2(height, height);
            startRect.anchoredPosition = new Vector2(8f, 0f);

            float pane = height * 0.22f;
            float gap = height * 0.04f;
            for (int i = 0; i < 4; i++)
            {
                Image square = UiKit.Image(startRect, "Pane" + i, StartBlue);
                RectTransform squareRect = square.rectTransform;
                squareRect.anchorMin = new Vector2(0.5f, 0.5f);
                squareRect.anchorMax = new Vector2(0.5f, 0.5f);
                squareRect.sizeDelta = new Vector2(pane, pane);
                float x = (i % 2 == 0 ? -1f : 1f) * (pane + gap) * 0.5f;
                float y = (i < 2 ? 1f : -1f) * (pane + gap) * 0.5f;
                squareRect.anchoredPosition = new Vector2(x, y);
            }

            _clockLabel = DesktopUi.Label(barRect, "Clock", string.Empty, 20, FontStyle.Normal, TextAnchor.MiddleRight, Color.white);
            RectTransform clockRect = _clockLabel.rectTransform;
            clockRect.anchorMin = new Vector2(1f, 0f);
            clockRect.anchorMax = new Vector2(1f, 1f);
            clockRect.pivot = new Vector2(1f, 0.5f);
            clockRect.sizeDelta = new Vector2(90f, 0f);
            clockRect.anchoredPosition = new Vector2(-16f, 0f);

            _stageLabel = DesktopUi.Label(barRect, "Stage", string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
            RectTransform stageRect = _stageLabel.rectTransform;
            stageRect.anchorMin = new Vector2(1f, 0f);
            stageRect.anchorMax = new Vector2(1f, 1f);
            stageRect.pivot = new Vector2(1f, 0.5f);
            stageRect.sizeDelta = new Vector2(320f, 0f);
            stageRect.anchoredPosition = new Vector2(-122f, 0f);
        }
#if UNITY_EDITOR
        // Only Start and its optional menu are authored; existing windows are untouched.
        public void AuthorStartMenu()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Start menu authoring requires Edit Mode.");
            var root = wallpaperImage != null ? wallpaperImage.transform.parent as RectTransform : null;
            var bar = root != null ? root.Find("Taskbar") as RectTransform : null;
            var start = bar != null ? bar.Find("Start") : null;
            var startImage = start != null ? start.GetComponent<Image>() : null;
            if (startImage == null) throw new InvalidOperationException("Authored Taskbar/Start Image is missing.");
            var startButton = start.GetComponent<Button>();
            if (startButton == null) startButton = start.gameObject.AddComponent<Button>();
            startButton.targetGraphic = startImage;
            startImage.raycastTarget = true;

            var menu = root.Find("StartMenu") as RectTransform;
            if (menu == null)
            {
                menu = UiKit.Image(root, "StartMenu", TaskbarColor).rectTransform;
                menu.anchorMin = menu.anchorMax = Vector2.zero;
                menu.pivot = Vector2.zero;
                menu.sizeDelta = new Vector2(240f, 172f);
                menu.anchoredPosition = new Vector2(8f, bar.rect.height + 8f);
            }
            menu.SetAsLastSibling();
            var code = AuthorStartMenuButton(menu, "ChungkangCode", "청강코드", 0);
            var settings = AuthorStartMenuButton(menu, "Settings", "설정", 1);
            var tutorial = AuthorStartMenuButton(menu, "Tutorial", "튜토리얼", 2);
            _startMenu = start.GetComponent<DesktopStartMenu>();
            if (_startMenu == null) _startMenu = start.gameObject.AddComponent<DesktopStartMenu>();
            _startMenu.ConfigureEditor(startButton, menu.gameObject, code, settings, tutorial);
            UnityEditor.EditorUtility.SetDirty(_startMenu);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        Button AuthorStartMenuButton(RectTransform menu, string name, string caption, int row)
        {
            var child = menu.Find(name);
            if (child != null)
            {
                var existing = child.GetComponent<Button>();
                if (existing == null) throw new InvalidOperationException("Start menu entry has no Button: " + name);
                return existing;
            }
            var button = UiKit.Button(menu, name, caption, new Color(1f, 1f, 1f, 0.08f), 20);
            DesktopUi.PlaceTopLeft(button.GetComponent<RectTransform>(), 8f, 8f + row * 52f, 224f, 48f);
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                var source = _stageLabel != null ? _stageLabel : _clockLabel;
                if (source != null) label.font = source.font;
                label.color = Color.white;
                label.raycastTarget = false;
            }
            return button;
        }
#endif
        public void CaptureEditorReferences()
        {
#if UNITY_EDITOR
            EnsureAppBindings();
            foreach (var binding in _appBindings)
            {
                if (binding == null || binding.Window == null) continue;
                binding.Window.InitializeAuthored();
                binding.Window.CaptureEditorReferences();
                UnityEditor.EditorUtility.SetDirty(binding.Window);
            }
            ValidateBindings(true);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        void OnValidate()
        {
            EnsureAppBindings();
            ValidateBindings(true);
        }
        void EnsureAppBindings()
        {
            if (_appBindings == null) _appBindings = new List<DesktopAppBinding>();
            if (_appBindings.Count != 0) return;
            if (_codeIcon == null && _settingsIcon == null && _tutorialIcon == null &&
                _code == null && _settingsWindow == null && _tutorial == null) return;
            _appBindings.Add(new DesktopAppBinding(_codeIcon, _code, new ChungkangCodeAppHandler()));
            _appBindings.Add(new DesktopAppBinding(_settingsIcon, _settingsWindow, new SettingsAppHandler()));
            _appBindings.Add(new DesktopAppBinding(_tutorialIcon, _tutorial, new TutorialAppHandler()));
        }
    }
}
