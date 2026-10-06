using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// Caller-supplied settlement for one stage. StageManager stores and announces it.
    /// It does not compute a grade.
    /// </summary>
    public sealed class ScoreSettlement
    {
        public ScoreSettlement(string stageId, int score, string summary = null)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("Stage id is required.", nameof(stageId));

            StageId = stageId.Trim();
            Score = score;
            Summary = summary ?? "";
        }

        public string StageId { get; }

        public int Score { get; }

        public string Summary { get; }
    }
}
