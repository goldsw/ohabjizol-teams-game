using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    public sealed class TutorialWindow : InfoWindow
    {
        static readonly Color InfoBack = new Color32(0xF2, 0xF3, 0xF5, 0xFF);
        static readonly Color Ink = new Color32(0x1E, 0x1F, 0x22, 0xFF);

        [SerializeField] Text _notice;
        [SerializeField] Button _start;
        [SerializeField] Button _skip;
        bool _allowSkip;
        public event Action StartRequested;
        public event Action SkipRequested;
        public bool HasAuthoredContent => _notice != null && _start != null && _skip != null;
        protected override void OnAuthoredInitialized()
        {
            if (!HasAuthoredContent)
            {
                if (Application.isPlaying) Debug.LogError("TutorialWindow authored content missing; bake in Edit Mode.", this);
                return;
            }
            _start.onClick.RemoveListener(RequestStart);
            _start.onClick.AddListener(RequestStart);
            _skip.onClick.RemoveListener(RequestSkip);
            _skip.onClick.AddListener(RequestSkip);
            ConfigureMode(_allowSkip);
        }
        void RequestStart() { StartRequested?.Invoke(); }
        void RequestSkip() { if (_allowSkip) SkipRequested?.Invoke(); }
        public void ConfigureMode(bool allowSkip)
        {
            _allowSkip = allowSkip;
            if (_skip != null) _skip.gameObject.SetActive(allowSkip);
            Refresh();
        }

        protected override void BuildInfo(RectTransform info)
        {
            Image back = UiKit.Image(info, "Back", InfoBack);
            UiKit.Stretch(back.rectTransform, 0f);

            _notice = DesktopUi.Label(info, "Notice", string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter, Ink);
            UiKit.Place(_notice.rectTransform, 0.06f, 0.55f, 0.94f, 0.92f);

            Button start = UiKit.Button(info, "Start", "튜토리얼 시작", DesktopUi.DiscordAccent, 24);
            _start = start;
            RectTransform startRect = start.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.3f);
            startRect.anchorMax = new Vector2(0.5f, 0.3f);
            startRect.pivot = new Vector2(0.5f, 0.5f);
            startRect.sizeDelta = new Vector2(260f, 64f);
            startRect.anchoredPosition = Vector2.zero;
            BuildSkip(info);
        }

        void BuildSkip(RectTransform info)
        {
            _skip = UiKit.Button(info, "Skip", "건너뛰기", DesktopUi.DiscordAccent, 24);
            UiKit.Place(_skip.GetComponent<RectTransform>(), 0.32f, 0.06f, 0.68f, 0.20f);
        }

#if UNITY_EDITOR
        public void BakePromptEditor()
        {
            if (Application.isPlaying) throw new InvalidOperationException("BakePromptEditor requires Edit Mode.");
            UnityEditor.Undo.RecordObject(this, "Bake tutorial prompt");
            // Existing authored windows already own the frame; only repair prompt content.
            InitializeAuthored();
            if (Info == null) throw new InvalidOperationException("Bake the tutorial window frame first.");
            if (_notice == null) _notice = Info.Find("Notice")?.GetComponent<Text>();
            if (_start == null) _start = Info.Find("Start")?.GetComponent<Button>();
            if (_skip == null) _skip = Info.Find("Skip")?.GetComponent<Button>();
            if (_notice == null)
            {
                _notice = DesktopUi.Label(Info, "Notice", string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter, Ink);
                UnityEditor.Undo.RegisterCreatedObjectUndo(_notice.gameObject, "Create tutorial notice");
                UiKit.Place(_notice.rectTransform, 0.06f, 0.55f, 0.94f, 0.92f);
            }
            if (_start == null)
            {
                _start = UiKit.Button(Info, "Start", "튜토리얼 시작", DesktopUi.DiscordAccent, 24);
                UnityEditor.Undo.RegisterCreatedObjectUndo(_start.gameObject, "Create tutorial start");
                UiKit.Place(_start.GetComponent<RectTransform>(), 0.32f, 0.26f, 0.68f, 0.44f);
            }
            if (_skip == null)
            {
                BuildSkip(Info);
                UnityEditor.Undo.RegisterCreatedObjectUndo(_skip.gameObject, "Create tutorial skip");
            }
            ButtonActiveBackground.Ensure(_start);
            ButtonActiveBackground.Ensure(_skip);
            ConfigureMode(false);
            foreach (var component in GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                UnityEditor.EditorUtility.SetDirty(component);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
#endif

        public override void Refresh()
        {
            if (_notice != null) _notice.text = _allowSkip
                ? "처음이신가요? 튜토리얼에서 면접 진행 방법을 배워 보세요."
                : "면접 진행 방법을 배웁니다.";
        }
    }
}
