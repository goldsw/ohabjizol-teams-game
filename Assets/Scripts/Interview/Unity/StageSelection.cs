using System;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Static slot from the menu into the interview. An empty slot reads the next saved stage.
    /// </summary>
    public static class StageSelection
    {
        static bool _hasValue;
        static StageId _current;

        public static bool HasValue
        {
            get { return _hasValue; }
        }

        public static StageId Current
        {
            get
            {
                if (StageExecutionPolicy.SingleStageMode)
                    return StageExecutionPolicy.Normalize(StageId.EntranceGameJam);
                return StageExecutionPolicy.Normalize(_hasValue ? _current : StageProgressStore.Load().NextStage);
            }
        }

        public static void Set(StageId id)
        {
            if (!Enum.IsDefined(typeof(StageId), id))
                throw new ArgumentOutOfRangeException(nameof(id));

            _current = StageExecutionPolicy.Normalize(id);
            _hasValue = true;
        }

        internal static void Clear()
        {
            _hasValue = false;
            _current = default(StageId);
        }
    }
}
