using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed partial class ResultBoard : MonoBehaviour
    {
        [SerializeField] RectTransform authoredRoot;
        [SerializeField] Text titleText;
        [SerializeField] Text completionText;
        [SerializeField] Text totalStarsText;
        [SerializeField] Text continueText;
        [SerializeField] Button continueButton;
        [SerializeField] Text[] emptyStars = new Text[5];
        [SerializeField] Text[] fullStars = new Text[5];
        [SerializeField] RectTransform[] starSlots = new RectTransform[5];
        [SerializeField] RectTransform[] starMasks = new RectTransform[5];
        InterviewResult result;
        public InterviewResult Result => result;
        public bool HasAuthoredUi => authoredRoot != null && titleText != null && completionText != null
            && totalStarsText != null && continueText != null && continueButton != null && StarsValid();
        bool StarsValid()
        {
            if (emptyStars == null || fullStars == null || starSlots == null || starMasks == null
                || emptyStars.Length != 5 || fullStars.Length != 5 || starSlots.Length != 5 || starMasks.Length != 5) return false;
            for (int i = 0; i < 5; i++)
                if (emptyStars[i] == null || fullStars[i] == null || starSlots[i] == null || starMasks[i] == null) return false;
            return true;
        }
        void Awake()
        {
            if (!HasAuthoredUi) return;
            continueButton.onClick.AddListener(Continue);
            ShowPending();
        }
        void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }
        public void ShowPending()
        {
            result = null;
            ShowSchedule(null);
            if (!HasAuthoredUi) return;
            titleText.text = "결과 준비중";
            completionText.text = string.Empty;
            totalStarsText.text = string.Empty;
            continueText.text = "메인으로";
            SetStars(0);
        }
        public void Show(InterviewResult value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            result = value;
            ShowSchedule(value);
            if (!HasAuthoredUi) return;
            var run = CampaignRunStore.Load();
            titleText.text = StageCatalog.Get(value.Stage).DisplayName;
            completionText.text = value.IsCleared ? "완료 · 클리어" : "완료";
            totalStarsText.text = "이번 단계 " + Stars(StarRatingRule.HalfUnits(value.AccuracyPercent))
                + "별\n누적 " + Stars(run.TotalHalfStars) + " / 20별";
            continueText.text = run.IsComplete ? "엔딩 보기" : "메인으로";
            SetStars(StarRatingRule.HalfUnits(value.AccuracyPercent));
        }
        static string Stars(int halves) => (halves / 2).ToString() + (halves % 2 == 0 ? "" : ".5");
        void SetStars(int halves)
        {
            for (int i = 0; i < 5; i++)
            {
                float fill = Mathf.Clamp01((halves - i * 2) / 2f);
                starMasks[i].anchorMax = new Vector2(fill, 1f);
                starMasks[i].gameObject.SetActive(fill > 0f);
            }
            ResizeStarGlyphs();
        }
        // Keep the full glyph at the authored slot width while only the mask is clipped.
        void LateUpdate() { if (HasAuthoredUi) ResizeStarGlyphs(); }
        void ResizeStarGlyphs()
        {
            for (int i = 0; i < 5; i++)
                fullStars[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, starSlots[i].rect.width);
        }
        void Continue()
        {
            bool complete = result != null && CampaignRunStore.Load().IsComplete;
            SceneManager.LoadScene(complete ? "EndingScene" : "MainMenuScene", LoadSceneMode.Single);
        }
        public void BakeAuthoredUi()
        {
#if UNITY_EDITOR
            if (authoredRoot != null) return;
            authoredRoot = ResultAuthoredUi.Canvas(transform);
            titleText = ResultAuthoredUi.Label(authoredRoot, "StageTitle", "결과 준비중", 56, new Vector2(.1f,.77f), new Vector2(.9f,.9f));
            completionText = ResultAuthoredUi.Label(authoredRoot, "Completion", "", 36, new Vector2(.1f,.65f), new Vector2(.9f,.75f));
            totalStarsText = ResultAuthoredUi.Label(authoredRoot, "TotalStars", "", 36, new Vector2(.1f,.25f), new Vector2(.9f,.4f));
            continueButton = ResultAuthoredUi.Button(authoredRoot, "ContinueButton", "메인으로", out continueText);
            emptyStars = new Text[5]; fullStars = new Text[5]; starSlots = new RectTransform[5]; starMasks = new RectTransform[5];
            for (int i = 0; i < 5; i++)
            {
                float left = .25f + i * .1f;
                var slot = ResultAuthoredUi.Rect(authoredRoot, "Star" + i, new Vector2(left,.43f), new Vector2(left+.1f,.60f));
                starSlots[i] = slot;
                emptyStars[i] = ResultAuthoredUi.Label(slot, "Empty", "☆", 120, Vector2.zero, Vector2.one);
                var mask = ResultAuthoredUi.Rect(slot, "FillMask", Vector2.zero, Vector2.one);
                mask.pivot = new Vector2(0f,.5f);
                mask.gameObject.AddComponent<RectMask2D>(); starMasks[i] = mask;
                var full = ResultAuthoredUi.Label(mask, "Full", "★", 120, Vector2.zero, new Vector2(0f,1f));
                full.rectTransform.pivot = new Vector2(0f,.5f);
                full.color = new Color(1f,.93f,.22f); fullStars[i] = full;
            }
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
