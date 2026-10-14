using System;
using UnityEngine;
namespace Ohabjizol.Interview
{
    public static class StageProgressStore
    {
        internal const string LegacyKey = "ohabjizol.stage-progress.v1";
        internal const string Key = "ohabjizol.stage-progress.v2";
        internal const string BackupKey = "ohabjizol.stage-progress.v1.backup";
        // Immutable v1 array mapping; never derive this from enum enumeration order.
        internal static readonly StageId[] LegacyOrder = { StageId.EntranceGameJam,
            StageId.GameProgrammingIntro, StageId.SecondYear, StageId.ThirdYear };
        public static StageProgress Load()
        {
            StageCompletionService.RecoverPending();
            return LoadWithoutRecovery();
        }
        internal static StageProgress LoadWithoutRecovery()
        {
            var flags = new bool[StageProgress.Count];
            var scores = new int[StageProgress.Count];
            for (int i = 0; i < scores.Length; i++) scores[i] = -1;
            var dto = ReadCurrent();
            if (dto != null)
            {
                foreach (var entry in dto.entries)
                {
                    if (entry == null) continue;
                    if (!Enum.TryParse<StageId>(entry.stageId, out var id)
                        || !Enum.IsDefined(typeof(StageId), id) || id.ToString() != entry.stageId) continue;
                    int index = (int)id;
                    if (index < 0 || index >= StageProgress.Count)
                        throw new InvalidOperationException("Stage progress domain capacity must include current stage IDs.");
                    flags[index] |= entry.cleared;
                    if (entry.best >= 0) scores[index] = Math.Max(scores[index], Math.Min(100, entry.best));
                }
                return StageProgress.FromRecord(flags, scores);
            }
            var legacy = Read<LegacyDto>(LegacyKey);
            if (legacy == null) return new StageProgress();
            for (int i = 0; i < LegacyOrder.Length; i++)
            {
                int index = (int)LegacyOrder[i];
                if (legacy.cleared != null && i < legacy.cleared.Length) flags[index] = legacy.cleared[i];
                if (legacy.best != null && i < legacy.best.Length && legacy.best[i] >= 0)
                    scores[index] = Math.Min(100, legacy.best[i]);
            }
            var migrated = StageProgress.FromRecord(flags, scores);
            if (!PlayerPrefs.HasKey(BackupKey))
            {
                PlayerPrefs.SetString(BackupKey, PlayerPrefs.GetString(LegacyKey, ""));
                PlayerPrefs.Save();
            }
            Save(migrated);
            return migrated;
        }
        // Existing v2 data is never overwritten by fallback migration. Unsupported or
        // malformed records stop loading and preserve the exact raw value for recovery.
        static ProgressDto ReadCurrent()
        {
            if (!PlayerPrefs.HasKey(Key)) return null;
            ProgressDto dto;
            try { dto = JsonUtility.FromJson<ProgressDto>(PlayerPrefs.GetString(Key, "")); }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Unreadable stage progress; original data preserved.", ex);
            }
            if (dto != null && dto.version > 2)
                throw new NotSupportedException("Stage progress was written by a newer version; original data preserved.");
            if (dto == null || dto.version != 2 || dto.entries == null)
                throw new InvalidOperationException("Invalid stage progress; original data preserved.");
            return dto;
        }
        static T Read<T>(string key) where T : class
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (ArgumentException) { return null; }
        }
        public static void Save(StageProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            ReadCurrent();
            var ids = (StageId[])Enum.GetValues(typeof(StageId));
            var dto = new ProgressDto { version = 2, entries = new Entry[ids.Length] };
            for (int i = 0; i < dto.entries.Length; i++)
                dto.entries[i] = new Entry { stageId = ids[i].ToString(),
                    cleared = progress.IsCleared(ids[i]),
                    best = Math.Max(-1, Math.Min(100, progress.BestAccuracy(ids[i]))) };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(dto));
            PlayerPrefs.Save();
        }
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(LegacyKey);
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(StageCompletionService.JournalKey);
            PlayerPrefs.Save();
        }
        [Serializable] sealed class LegacyDto { public bool[] cleared; public int[] best; }
        [Serializable] sealed class ProgressDto { public int version; public Entry[] entries; }
        [Serializable] sealed class Entry { public string stageId; public bool cleared; public int best; }
    }
}
