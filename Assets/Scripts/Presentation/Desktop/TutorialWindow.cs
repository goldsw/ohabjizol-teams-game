using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Placeholder. The start button only changes the notice line.
    /// </summary>
    public sealed class TutorialWindow : InfoWindow
    {
        static readonly Color InfoBack = new Color32(0xF2, 0xF3, 0xF5, 0xFF);
        static readonly Color Ink = new Color32(0x1E, 0x1F, 0x22, 0xFF);

        [SerializeField] Text _notice;
        [SerializeField] Button _start;
        public bool HasAuthoredContent => _notice != null && _start != null;
        protected override void OnAuthoredInitialized()
        {
            if (!HasAuthoredContent) { Debug.LogError("TutorialWindow authored content missing; bake in Edit Mode.", this); return; }
            _start.onClick.RemoveListener(ShowNotice);
            _start.onClick.AddListener(ShowNotice);
        }
        void ShowNotice() { _notice.text = "준비 중입니다."; }

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

        }

        public override void Refresh()
        {
            if (_notice != null) _notice.text = "면접 진행 방법을 배웁니다.";
        }
    }
}
