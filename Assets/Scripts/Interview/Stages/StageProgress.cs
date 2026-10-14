using System;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Clear flags and best accuracy for each <see cref="StageId"/>.
    /// A clear stays set. Best accuracy only moves upward.
    /// </summary>
    public sealed class StageProgress
    {
        internal const int Count = 4;

        readonly bool[] _cleared = new bool[Count];
        readonly int[] _best;

        public StageProgress()
        {
            _best = new int[Count];
            for (int i = 0; i < Count; i++)
                _best[i] = -1;
        }

        public bool IsCleared(StageId id)
        {
            return _cleared[Index(id)];
        }

        public int BestAccuracy(StageId id)
        {
            return _best[Index(id)];
        }

        public bool IsPlayable(StageId id)
        {
            int index = Index(id);
            if (index == 0)
                return true;
            return _cleared[index - 1];
        }

        public StageId NextStage
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!_cleared[i])
                        return (StageId)i;
                }

                return StageId.ThirdYear;
            }
        }

        public bool AllCleared
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!_cleared[i])
                        return false;
                }

                return true;
            }
        }

        public void Record(StageId id, int accuracyPercent)
        {
            int index = Index(id);
            if (_best[index] < accuracyPercent)
                _best[index] = accuracyPercent;
            if (StageAccuracyRule.IsClear(accuracyPercent))
                _cleared[index] = true;
        }

        internal static StageProgress FromRecord(bool[] cleared, int[] best)
        {
            var progress = new StageProgress();
            for (int i = 0; i < Count; i++)
            {
                progress._cleared[i] = cleared[i];
                progress._best[i] = best[i];
            }

            return progress;
        }

        internal void CopyRecord(bool[] cleared, int[] best)
        {
            for (int i = 0; i < Count; i++)
            {
                cleared[i] = _cleared[i];
                best[i] = _best[i];
            }
        }

        static int Index(StageId id)
        {
            int index = (int)id;
            if (index < 0 || index >= Count || !Enum.IsDefined(typeof(StageId), id))
                throw new ArgumentOutOfRangeException(nameof(id));
            return index;
        }
    }
}
