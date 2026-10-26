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
        // After completion this remains the last active stage; callers must check IsComplete first.
        public StageId NextStage
        {
            get
            {
                var stages = StageExecutionPolicy.ActiveStages;
                for (int i = 0; i < stages.Count; i++) if (!completed[(int)stages[i]]) return stages[i];
                return stages[stages.Count - 1];
            }
        }
        public bool IsComplete
        {
            get
            {
                var stages = StageExecutionPolicy.ActiveStages;
                for (int i = 0; i < stages.Count; i++) if (!completed[(int)stages[i]]) return false;
                return true;
            }
        }
        public int TotalHalfStars
        {
            get
            {
                int sum = 0;
                var stages = StageExecutionPolicy.ActiveStages;
                for (int i = 0; i < stages.Count; i++)
                {
                    int index = (int)stages[i];
                    if (completed[index]) sum += StarRatingRule.HalfUnits(percentages[index]);
                }
                return sum;
            }
        }
        // A provisional ending until all active stages are complete. Incomplete stages count as zero stars.
        public EndingKind Ending => ResolveEnding();
        public EndingKind ResolveEnding(float goodStars = 3.5f, float normalStars = 2.5f)
        {
            if (float.IsNaN(goodStars) || float.IsNaN(normalStars)
                || normalStars < 0f || goodStars < normalStars || goodStars > 5f)
                throw new ArgumentOutOfRangeException(nameof(goodStars));
            var stages = StageExecutionPolicy.ActiveStages;
            int halfStars = TotalHalfStars;
            return halfStars >= goodStars * 2f * stages.Count ? EndingKind.Good
                : halfStars >= normalStars * 2f * stages.Count ? EndingKind.Normal : EndingKind.Bad;
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
