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
                if (!_hasValue)
                    return StageProgressStore.Load().NextStage;
                return _current;
            }
        }

        public static void Set(StageId id)
        {
            if (!Enum.IsDefined(typeof(StageId), id))
                throw new ArgumentOutOfRangeException(nameof(id));

            _current = id;
            _hasValue = true;
        }

        internal static void Clear()
        {
            _hasValue = false;
            _current = default(StageId);
        }
    }
}
