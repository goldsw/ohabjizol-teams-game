using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ohabjizol.Interview
{
    [CreateAssetMenu(menuName = "Interview/Stage Schedule Config")]
    public sealed class StageScheduleConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry {
            public StageId StageId;
            [Range(1, 30)] public int ProductionDays = 15;
        }
        [SerializeField] List<Entry> _entries = DefaultEntries();
        public IReadOnlyList<Entry> Entries => _entries;
        public int GetProductionDays(StageId stageId)
        {
            if (!Enum.IsDefined(typeof(StageId), stageId)) throw new ArgumentOutOfRangeException(nameof(stageId));
            if (_entries != null)
                foreach (var entry in _entries)
                    if (entry != null && entry.StageId.Equals(stageId)) return Mathf.Clamp(entry.ProductionDays, 1, 30);
            return 15;
        }
        static List<Entry> DefaultEntries()
        {
            var entries = new List<Entry>();
            foreach (StageId stage in Enum.GetValues(typeof(StageId)))
                entries.Add(new Entry { StageId = stage, ProductionDays = 15 });
            return entries;
        }
    }
}