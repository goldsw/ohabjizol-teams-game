using System;
using UnityEngine;
namespace Ohabjizol.Interview
{
    [Serializable]
    public sealed class StageStatsEntry
    {
        public StageId stageId;
        public bool useOverride;
        public string[] characterIds = new string[0];
        public int requiredPlanning, requiredProgramming, requiredGraphics;
    }
    [CreateAssetMenu(fileName = "StageStatsCatalog", menuName = "Ohabjizol/Stage Stats Catalog")]
    public sealed class StageStatsCatalogAsset : ScriptableObject
    {
        public StageStatsEntry[] stages = new StageStatsEntry[0];
        public StageStatsEntry Find(StageId id)
        {
            if (stages != null) foreach (var stage in stages) if (stage != null && stage.stageId == id) return stage;
            return null;
        }
    }
}