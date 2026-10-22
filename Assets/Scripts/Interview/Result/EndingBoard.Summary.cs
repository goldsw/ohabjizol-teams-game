using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed partial class EndingBoard
    {
        [SerializeField] TenPointStarsView endingStars;
        [SerializeField] RectTransform endingStarsRoot;
        InterviewResult endingResult;

        public InterviewResult Result => endingResult;
        public bool HasSummaryUi => endingStars != null && endingStars.HasAuthoredUi && endingStarsRoot != null;

        void ShowResultSummary(InterviewResult value, int totalHalfStars)
        {
            endingResult = value;
            bool showFiveSlots = UsesReferenceDesign && useFixedKind
                && (fixedKind == EndingKind.Normal || fixedKind == EndingKind.Bad);
            if (endingStarsRoot != null) endingStarsRoot.gameObject.SetActive(value != null || showFiveSlots);
            if (endingStars != null)
            {
                if (value == null) endingStars.SetUnavailable();
                else endingStars.SetScore(value.HalfStarUnits);
            }
            if (value != null && totalStarsText != null)
                totalStarsText.text = "이번 결과 " + SummaryStars(value.HalfStarUnits) + " / 5별\n누적 "
                    + SummaryStars(totalHalfStars) + " / " + (StageExecutionPolicy.ActiveStages.Count * 5) + "별";
        }

        static string SummaryStars(int halves) => (halves / 2) + (halves % 2 == 0 ? "" : ".5");
    }
}
