using System;
namespace Ohabjizol.Interview
{
    public sealed class CampaignRunProgress
    {
        public const int StageCount = 4;
        readonly bool[] completed = new bool[StageCount];
        readonly int[] percentages = new int[StageCount];
        public CampaignRunProgress() : this(Guid.NewGuid().ToString("N")) { }
        public CampaignRunProgress(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run needs an id.", nameof(runId));
            RunId = runId;
        }
        public string RunId { get; }
        // After completion this remains ThirdYear; callers must check IsComplete first.
        public StageId NextStage
        {
            get
            {
                for (int i = 0; i < StageCount; i++) if (!completed[i]) return (StageId)i;
                return StageId.ThirdYear;
            }
        }
        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < StageCount; i++) if (!completed[i]) return false;
                return true;
            }
        }
        public int TotalHalfStars
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < StageCount; i++) if (completed[i]) sum += StarRatingRule.HalfUnits(percentages[i]);
                return sum;
            }
        }
        // A provisional ending until all four stages are complete.
        public EndingKind Ending => ResolveEnding();
        public EndingKind ResolveEnding(int goodHalfThreshold = 30, int normalHalfThreshold = 20)
        {
            if (normalHalfThreshold < 0 || goodHalfThreshold < normalHalfThreshold || goodHalfThreshold > 40)
                throw new ArgumentOutOfRangeException(nameof(goodHalfThreshold));
            return TotalHalfStars >= goodHalfThreshold ? EndingKind.Good
                : TotalHalfStars >= normalHalfThreshold ? EndingKind.Normal : EndingKind.Bad;
        }
        public bool IsCompleted(StageId stage) => completed[Index(stage)];
        internal int Accuracy(StageId stage) => percentages[Index(stage)];
        public bool Record(StageId stage, int percent)
        {
            int index = Index(stage);
            if (completed[index]) return false;
            percentages[index] = Math.Max(0, Math.Min(100, percent));
            completed[index] = true;
            return true;
        }
        internal void CopyRecord(bool[] flags, int[] scores)
        {
            Array.Copy(completed, flags, StageCount);
            Array.Copy(percentages, scores, StageCount);
        }
        internal static CampaignRunProgress FromRecord(string runId, bool[] flags, int[] scores)
        {
            if (flags == null || scores == null || flags.Length != StageCount || scores.Length != StageCount)
                throw new ArgumentException("A run has four stage records.");
            var run = new CampaignRunProgress(runId);
            for (int i = 0; i < StageCount; i++)
                if (flags[i]) run.Record((StageId)i, scores[i]);
            return run;
        }
        static int Index(StageId stage)
        {
            int index = (int)stage;
            if (index < 0 || index >= StageCount) throw new ArgumentOutOfRangeException(nameof(stage));
            return index;
        }
    }
}
