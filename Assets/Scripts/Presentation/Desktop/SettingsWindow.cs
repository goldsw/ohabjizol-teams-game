using System;
using Ohabjizol.Managers;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;
using DisplaySize = Ohabjizol.Managers.Resolution;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Volume applies while dragging. Resolution and window mode wait for the apply button.
    /// Progress reset needs a second press. Each open reloads the stored choices.
    /// </summary>
    public sealed class SettingsWindow : InfoWindow
    {
        const float Left = 32f;
        const float LabelWidth = 150f;
        const float RowHeight = 56f;

        static readonly Color InfoBack = new Color32(0xF2, 0xF3, 0xF5, 0xFF);
        static readonly Color Ink = new Color32(0x1E, 0x1F, 0x22, 0xFF);
        static readonly Color InkMuted = new Color32(0x5C, 0x5E, 0x66, 0xFF);

        SettingsApplier _applier;
        Action _progressReset;
        [SerializeField] Slider _volume;
        [SerializeField] Text _volumeValue;
        [SerializeField] Text _resolution;
        [SerializeField] Text _modeLabel;
        [SerializeField] Text _applyStatus;
        [SerializeField] Text _resetLabel;
        int _resolutionIndex;
        WindowMode _mode;
        bool _resetArmed;
        bool _refreshing;

        [SerializeField] Button[] _buttons;
        public bool HasAuthoredContent => _volume != null && _volumeValue != null && _resolution != null &&
            _modeLabel != null && _applyStatus != null && _resetLabel != null && _buttons != null &&
            _buttons.Length == 6 && Array.TrueForAll(_buttons, button => button != null);
        public override void CaptureEditorReferences()
        {
            base.CaptureEditorReferences();
#if UNITY_EDITOR
            if (Info == null) return;
            _buttons = new Button[] { Info.Find("Previous")?.GetComponent<Button>(), Info.Find("Next")?.GetComponent<Button>(),
                Info.Find("Mode")?.GetComponent<Button>(), Info.Find("Apply")?.GetComponent<Button>(),
                Info.Find("ResetProgress")?.GetComponent<Button>(), Info.Find("ShutDown")?.GetComponent<Button>() };
#endif
        }
        protected override void OnAuthoredInitialized()
        {
            if (!HasAuthoredContent) { Debug.LogError("SettingsWindow authored content missing; bake in Edit Mode.", this); return; }
            _volume.onValueChanged.RemoveListener(HandleVolume);
            _volume.onValueChanged.AddListener(HandleVolume);

            UnityEngine.Events.UnityAction[] actions = { PreviousResolution, NextResolution, ToggleMode, ApplyDisplay, PressReset, SettingsApplier.ShutDown };
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].onClick.RemoveListener(actions[i]);
                _buttons[i].onClick.AddListener(actions[i]);
            }
        }
        void PreviousResolution() { StepResolution(-1); }
        void NextResolution() { StepResolution(1); }
        public void Bind(SettingsApplier applier, Action progressReset)
        {
            _applier = applier;
            _progressReset = progressReset;
            Closed -= HandleClosed;
            Closed += HandleClosed;
        }

        public void Unbind()
        {
            Closed -= HandleClosed;
            _progressReset = null;
        }
        protected override void BuildInfo(RectTransform info)
        {
            Image back = UiKit.Image(info, "Back", InfoBack);
            UiKit.Stretch(back.rectTransform, 0f);

            float y = 28f;
            RowLabel(info, "사운드", y);
            _volume = CreateSlider(info, Left + LabelWidth, y + 18f, 300f, 20f);

            _volumeValue = DesktopUi.Label(info, "VolumeValue", string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
            DesktopUi.PlaceTopLeft(_volumeValue.rectTransform, Left + LabelWidth + 320f, y, 100f, RowHeight);

            y += 76f;
            RowLabel(info, "해상도", y);
            Button previous = SmallButton(info, "Previous", "◀", Left + LabelWidth, y + 4f, 48f);

            _resolution = DesktopUi.Label(info, "Resolution", string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Ink);
            DesktopUi.PlaceTopLeft(_resolution.rectTransform, Left + LabelWidth + 56f, y, 196f, RowHeight);
            Button next = SmallButton(info, "Next", "▶", Left + LabelWidth + 260f, y + 4f, 48f);


            y += 76f;
            RowLabel(info, "창 모드", y);
            Button mode = SmallButton(info, "Mode", string.Empty, Left + LabelWidth, y + 4f, 308f);

            _modeLabel = DesktopUi.ButtonLabel(mode);

            y += 76f;
            Button apply = UiKit.Button(info, "Apply", "적용", DesktopUi.DiscordAccent, 22);
            DesktopUi.PlaceTopLeft(apply.GetComponent<RectTransform>(), Left + LabelWidth, y, 160f, 52f);

            _applyStatus = DesktopUi.Label(info, "ApplyStatus", string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleLeft, InkMuted);
            DesktopUi.PlaceTopLeft(_applyStatus.rectTransform, Left + LabelWidth + 176f, y, 300f, 52f);

            y += 84f;
            Image rule = UiKit.Image(info, "Rule", new Color(0f, 0f, 0f, 0.12f));
            RectTransform ruleRect = rule.rectTransform;
            ruleRect.anchorMin = new Vector2(0f, 1f);
            ruleRect.anchorMax = new Vector2(1f, 1f);
            ruleRect.pivot = new Vector2(0.5f, 1f);
            ruleRect.offsetMin = new Vector2(Left, -y - 2f);
            ruleRect.offsetMax = new Vector2(-Left, -y);

            y += 24f;
            RowLabel(info, "진행", y);
            Button reset = UiKit.Button(info, "ResetProgress", string.Empty, DesktopUi.WarnOrange, 20);
            DesktopUi.PlaceTopLeft(reset.GetComponent<RectTransform>(), Left + LabelWidth, y, 308f, 52f);

            _resetLabel = DesktopUi.ButtonLabel(reset);

            y += 76f;
            RowLabel(info, "전원", y);
            Button shutDown = UiKit.Button(info, "ShutDown", "컴퓨터 종료", DesktopUi.DangerRed, 22);
            DesktopUi.PlaceTopLeft(shutDown.GetComponent<RectTransform>(), Left + LabelWidth, y, 308f, 52f);

        }

        public override void Refresh()
        {
            _resetArmed = false;
            if (_resetLabel != null)
                _resetLabel.text = "최고 기록 초기화";
            if (_applyStatus != null)
                _applyStatus.text = string.Empty;
            if (_applier == null)
                return;

            SettingsManager settings = _applier.Settings;
            _refreshing = true;
            _volume.value = settings.Volume;
            _refreshing = false;
            ShowVolume(settings.Volume);
            _resolutionIndex = SettingsApplier.IndexOf(settings.Resolution.Width, settings.Resolution.Height);
            _mode = settings.WindowMode == WindowMode.Windowed ? WindowMode.Windowed : WindowMode.Fullscreen;
            ShowDisplay();
        }

        void HandleVolume(float value)
        {
            if (_refreshing || _applier == null)
                return;

            _applier.SetVolume(value);
            ShowVolume(_applier.Settings.Volume);
        }

        void ShowVolume(float value)
        {
            _volumeValue.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        void StepResolution(int step)
        {
            int count = SettingsApplier.Resolutions.Count;
            _resolutionIndex = ((_resolutionIndex + step) % count + count) % count;
            ShowDisplay();
            _applyStatus.text = "적용을 눌러 바꿉니다";
        }

        void ToggleMode()
        {
            _mode = _mode == WindowMode.Windowed ? WindowMode.Fullscreen : WindowMode.Windowed;
            ShowDisplay();
            _applyStatus.text = "적용을 눌러 바꿉니다";
        }

        void ShowDisplay()
        {
            DisplaySize size = SettingsApplier.Resolutions[_resolutionIndex];
            _resolution.text = size.Width + " × " + size.Height;
            _modeLabel.text = _mode == WindowMode.Windowed ? "창" : "전체 화면";
        }

        void ApplyDisplay()
        {
            if (_applier == null)
                return;

            _applier.ApplyDisplay(SettingsApplier.Resolutions[_resolutionIndex], _mode);
            _applyStatus.text = Application.isEditor ? "저장됨 (에디터에서는 화면이 바뀌지 않음)" : "적용됨";
        }

        void PressReset()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                _resetLabel.text = "한 번 더 누르면 초기화";
                return;
            }

            _resetArmed = false;
            SettingsApplier.ResetProgress();
            _resetLabel.text = "초기화됨";
            if (_progressReset != null)
                _progressReset();
        }

        void HandleClosed(WindowToolbar window)
        {
            if (_applier != null)
                _applier.Flush();
        }

        static void RowLabel(RectTransform info, string value, float y)
        {
            Text label = DesktopUi.Label(info, "Label " + value, value, 24, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
            DesktopUi.PlaceTopLeft(label.rectTransform, Left, y, LabelWidth, RowHeight);
        }

        static Button SmallButton(RectTransform info, string name, string caption, float x, float y, float width)
        {
            Button button = UiKit.Button(info, name, caption, DesktopUi.NeutralButton, 22);
            DesktopUi.PlaceTopLeft(button.GetComponent<RectTransform>(), x, y, width, 48f);
            return button;
        }

        static Slider CreateSlider(RectTransform parent, float x, float y, float width, float height)
        {
            var sliderObject = new GameObject("Volume", typeof(RectTransform));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            DesktopUi.PlaceTopLeft(rect, x, y, width, height);

            Image track = UiKit.Image(rect, "Track", new Color(0f, 0f, 0f, 0.18f));
            track.raycastTarget = true;
            UiKit.Stretch(track.rectTransform, 0f);
            track.rectTransform.offsetMin = new Vector2(0f, height * 0.3f);
            track.rectTransform.offsetMax = new Vector2(0f, -height * 0.3f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(rect, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            UiKit.Stretch(fillAreaRect, 0f);
            fillAreaRect.offsetMin = new Vector2(0f, height * 0.3f);
            fillAreaRect.offsetMax = new Vector2(0f, -height * 0.3f);
            Image fill = UiKit.Image(fillAreaRect, "Fill", DesktopUi.DiscordAccent);
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(rect, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            UiKit.Stretch(handleAreaRect, 0f);
            handleAreaRect.offsetMin = new Vector2(height * 0.5f, 0f);
            handleAreaRect.offsetMax = new Vector2(-height * 0.5f, 0f);
            Image handle = UiKit.Image(handleAreaRect, "Handle", Color.white);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(height * 1.4f, height * 1.4f);
            Outline outline = handle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.35f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.None;
            slider.navigation = navigation;
            return slider;
        }
    }
}
