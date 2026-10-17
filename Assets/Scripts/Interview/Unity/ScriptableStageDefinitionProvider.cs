using System;
using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public sealed class ScriptableStageDefinitionProvider : IStageDefinitionProvider
    {
        readonly ApplicantCatalogAsset applicants;
        readonly StageStatsCatalogAsset stages;
        readonly Random random = new Random();
        public ScriptableStageDefinitionProvider(ApplicantCatalogAsset applicants, StageStatsCatalogAsset stages)
        {
            this.applicants = applicants != null ? applicants : throw new ArgumentNullException(nameof(applicants));
            this.stages = stages != null ? stages : throw new ArgumentNullException(nameof(stages));
        }
        public StageDefinition Get(StageId id)
        {
            var original = StageCatalog.GetPrototype(id);
            var entry = stages.Find(id);
            if (entry == null || !entry.useOverride) return original;
            if (entry.requiredPlanning < 0 || entry.requiredProgramming < 0 || entry.requiredGraphics < 0) throw new InvalidOperationException("Negative requirements.");
            var people = new Applicant[entry.characterIds.Length];
            var unique = new HashSet<string>();
            for (int i = 0; i < people.Length; i++)
            {
                if (!unique.Add(entry.characterIds[i])) throw new InvalidOperationException("Duplicate character ID.");
                people[i] = applicants.Find(entry.characterIds[i]).Sample(random);
            }
            return new StageDefinition(id, original.DisplayName,
                new GameJamTheme(original.Theme.DisplayName, new SkillSet(entry.requiredPlanning, entry.requiredProgramming, entry.requiredGraphics), original.Theme.CoreRole), people);
        }
        public IReadOnlyList<StageDefinition> All
        {
            get
            {
                var result = new StageDefinition[StageCatalog.Prototypes.Count];
                for (int i = 0; i < result.Length; i++) result[i] = Get(StageCatalog.Prototypes[i].Id);
                return Array.AsReadOnly(result);
            }
        }
    }
}