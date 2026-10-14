using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public static class StageCatalog
    {
        public static IReadOnlyList<StageDefinition> All { get; } = new StageDefinition[]
        {
            new StageDefinition(
                StageId.EntranceGameJam,
                "입학 게임잼",
                PrototypeCatalog.Theme,
                PrototypeCatalog.Applicants),
            StagePrototypeData.GameProgrammingIntro(),
            StagePrototypeData.SecondYear(),
            StagePrototypeData.ThirdYear()
        };

        public static StageDefinition Get(StageId id)
        {
            int index = (int)id;
            if (index < 0 || index >= All.Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            StageDefinition stage = All[index];
            if (stage.Id != id)
                throw new ArgumentOutOfRangeException(nameof(id));
            return stage;
        }
    }
}
