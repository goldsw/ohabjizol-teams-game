using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public static class StageExecutionPolicy
    {
        // Set false here to restore execution of all four stages and their original save keys.
        public static readonly bool SingleStageMode = true;

        public static IReadOnlyList<StageId> ActiveStages { get; } = Array.AsReadOnly(
            SingleStageMode ? new[] { StageId.EntranceGameJam }
                : (StageId[])Enum.GetValues(typeof(StageId)));

        public static StageId Normalize(StageId stage)
        {
            Validate(stage);
            return SingleStageMode ? StageId.EntranceGameJam : stage;
        }

        public static int TeamCapacity(StageId stage)
        {
            Validate(stage);
            return stage == StageId.EntranceGameJam ? 6 : 4;
        }

        public static string PersistenceKey(string legacyKey)
        {
            if (string.IsNullOrEmpty(legacyKey)) throw new ArgumentException("A save key is required.", nameof(legacyKey));
            return SingleStageMode ? legacyKey + ".entrance-single" : legacyKey;
        }

        static void Validate(StageId stage)
        {
            if (!Enum.IsDefined(typeof(StageId), stage))
                throw new ArgumentOutOfRangeException(nameof(stage));
        }
    }
}
