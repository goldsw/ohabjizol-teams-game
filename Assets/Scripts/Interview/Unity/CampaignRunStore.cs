using System;
using UnityEngine;
namespace Ohabjizol.Interview
{
    public static class CampaignRunStore
    {
        internal const string LegacyKey = "ohabjizol.campaign-run.v1";
        internal static string Key => StageExecutionPolicy.PersistenceKey(LegacyKey);
        public static CampaignRunProgress Load()
        {
            StageCompletionService.RecoverPending();
            return LoadWithoutRecovery();
        }
        internal static CampaignRunProgress LoadWithoutRecovery(bool requireExisting = false)
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var dto = JsonUtility.FromJson<RunDto>(json);
                    if (dto != null && dto.version == 1)
                        return CampaignRunProgress.FromRecord(dto.runId, dto.completed, dto.percentages);
                }
                catch (ArgumentException) { }
            }
            if (requireExisting) throw new InvalidOperationException("Current campaign record is missing or invalid.");
            // Persist the fallback so repeated loads share the same run identity.
            var fallback = new CampaignRunProgress();
            Save(fallback);
            return fallback;
        }
        public static void Save(CampaignRunProgress run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var dto = new RunDto
            {
                version = 1, runId = run.RunId,
                completed = new bool[CampaignRunProgress.StageCount],
                percentages = new int[CampaignRunProgress.StageCount]
            };
            run.CopyRecord(dto.completed, dto.percentages);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(dto));
            PlayerPrefs.Save();
        }
        public static CampaignRunProgress StartNewRun()
        {
            StageCompletionService.RecoverPending();
            var run = new CampaignRunProgress();
            Save(run);
            return run;
        }
        public static bool Record(InterviewResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var run = Load();
            bool recorded = !run.IsCompleted(result.Stage);
            StageCompletionService.Complete(run.RunId, result);
            return recorded;
        }
        [Serializable]
        sealed class RunDto
        {
            public int version;
            public string runId;
            public bool[] completed;
            public int[] percentages;
        }
    }
}
