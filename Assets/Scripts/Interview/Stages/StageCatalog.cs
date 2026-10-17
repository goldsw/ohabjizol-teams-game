using System;
using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public static class StageCatalog
    {
        static IStageDefinitionProvider provider;
        static readonly IReadOnlyList<StageDefinition> prototypes = Array.AsReadOnly(new[]
        {
            new StageDefinition(StageId.EntranceGameJam, "입학 게임잼", PrototypeCatalog.Theme, PrototypeCatalog.Applicants),
            StagePrototypeData.GameProgrammingIntro(), StagePrototypeData.SecondYear(), StagePrototypeData.ThirdYear()
        });
        public static void Configure(IStageDefinitionProvider value) { provider = value; }
        public static IReadOnlyList<StageDefinition> All => provider == null ? prototypes : provider.All;
        public static StageDefinition Get(StageId id) => provider == null ? GetPrototype(id) : provider.Get(id);
        internal static IReadOnlyList<StageDefinition> Prototypes => prototypes;
        internal static StageDefinition GetPrototype(StageId id)
        {
            int index = (int)id;
            if (index < 0 || index >= prototypes.Count || prototypes[index].Id != id)
                throw new ArgumentOutOfRangeException(nameof(id));
            return prototypes[index];
        }
    }
}