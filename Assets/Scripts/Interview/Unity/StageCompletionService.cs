using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ohabjizol.Interview
{
    public static class StageCompletionService
    {
        internal const string JournalKey = "ohabjizol.stage-completion-journal.v1";
        public static IReadOnlyList<string> PersistenceKeys { get; } = Array.AsReadOnly(new[] {
            StageProgressStore.LegacyKey, StageProgressStore.Key, StageProgressStore.BackupKey,
            CampaignRunStore.Key, JournalKey });

        public static void Complete(string runId, InterviewResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run needs an id.", nameof(runId));
            RecoverPending();
            var run = CampaignRunStore.LoadWithoutRecovery(true);
            var journal = new Journal { version = 1, runId = runId,
                stageId = result.Stage.ToString(), percentage = Math.Max(0, Math.Min(100, result.AccuracyPercent)) };
            Validate(run, journal);
            // Durable intent precedes both stores. Every subsequent write can be replayed.
            PlayerPrefs.SetString(JournalKey, JsonUtility.ToJson(journal));
            PlayerPrefs.Save();
            Apply(run, journal);
        }

        public static void RecoverPending()
        {
            if (!PlayerPrefs.HasKey(JournalKey)) return;
            Journal journal;
            try { journal = JsonUtility.FromJson<Journal>(PlayerPrefs.GetString(JournalKey, "")); }
            catch (ArgumentException ex) { throw new InvalidOperationException("Unreadable completion journal.", ex); }
            if (journal == null || journal.version != 1 || string.IsNullOrWhiteSpace(journal.runId)
                || journal.percentage < 0 || journal.percentage > 100)
                throw new InvalidOperationException("Invalid completion journal.");
            var run = CampaignRunStore.LoadWithoutRecovery(true);
            if (run.RunId != journal.runId)
            {
                // Superseded transactions must not change either store in the new run.
                ClearJournal();
                return;
            }
            Validate(run, journal);
            Apply(run, journal);
        }

        static StageId Stage(Journal journal)
        {
            if (Enum.TryParse<StageId>(journal.stageId, out var id)
                && Enum.IsDefined(typeof(StageId), id) && id.ToString() == journal.stageId) return id;
            throw new InvalidOperationException("Unknown completion stage.");
        }
        static void Validate(CampaignRunProgress run, Journal journal)
        {
            if (run.RunId != journal.runId) throw new InvalidOperationException("Completion belongs to another run.");
            var stage = Stage(journal);
            // Check duplicates before NextStage: the campaign has already advanced on retry.
            if (run.IsCompleted(stage))
            {
                if (run.Accuracy(stage) != journal.percentage)
                    throw new InvalidOperationException("Stage already completed with a different score.");
                return;
            }
            if (run.IsComplete || run.NextStage != stage)
                throw new InvalidOperationException("Completion is not the current stage.");
        }
        static void Apply(CampaignRunProgress run, Journal journal)
        {
            var stage = Stage(journal);
            var progress = StageProgressStore.LoadWithoutRecovery();
            progress.Record(stage, journal.percentage);
            StageProgressStore.Save(progress);
            run.Record(stage, journal.percentage);
            CampaignRunStore.Save(run);
            ClearJournal();
        }
        static void ClearJournal()
        {
            PlayerPrefs.DeleteKey(JournalKey);
            PlayerPrefs.Save();
        }
        [Serializable] sealed class Journal
        {
            public int version;
            public string runId;
            public string stageId;
            public int percentage;
        }
    }
}
