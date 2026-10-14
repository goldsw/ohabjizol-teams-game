using System;

namespace Ohabjizol.Interview
{
    // Captured once at entry; document identities never consult mutable global selection.
    public sealed class StageContext
    {
        public StageContext(string runId, StageDefinition stage)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run identity is required.", nameof(runId));
            RunId = runId;
            StageDefinition = stage ?? throw new ArgumentNullException(nameof(stage));
        }

        public string RunId { get; }
        public StageDefinition StageDefinition { get; }
        public string ApplicantKey(int currentIndex) => RunId + "." + StageDefinition.Id + "." + (currentIndex + 1);
    }
}