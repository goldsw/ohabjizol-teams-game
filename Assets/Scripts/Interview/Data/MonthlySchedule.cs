using System;

namespace Ohabjizol.Interview
{
    /// <summary>Thirty generic markers; default is an empty schedule.</summary>
    public readonly struct MonthlySchedule
    {
        readonly bool[] _days;
        public MonthlySchedule(bool[] days)
        {
            if (days == null) throw new ArgumentNullException(nameof(days));
            if (days.Length != 30) throw new ArgumentException("A month has 30 markers.", nameof(days));
            _days = (bool[])days.Clone();
        }
        public int Count
        {
            get
            {
                int count = 0;
                if (_days != null)
                    for (int i = 0; i < 30; i++) if (_days[i]) count++;
                return count;
            }
        }
        public bool IsMarked(int day1based)
        {
            if (day1based < 1 || day1based > 30) throw new ArgumentOutOfRangeException(nameof(day1based));
            return _days != null && _days[day1based - 1];
        }
        public int[] GetMarkedDays()
        {
            var result = new int[Count];
            int index = 0;
            for (int day = 1; day <= 30; day++) if (IsMarked(day)) result[index++] = day;
            return result;
        }
        public static MonthlySchedule CreateRandom(int count, Random random)
        {
            if (count < 0 || count > 30) throw new ArgumentOutOfRangeException(nameof(count));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var pool = new int[30];
            var days = new bool[30];
            for (int i = 0; i < 30; i++) pool[i] = i;
            for (int i = 0; i < count; i++)
            {
                int selected = random.Next(i, 30);
                int value = pool[selected];
                pool[selected] = pool[i];
                pool[i] = value;
                days[value] = true;
            }
            return new MonthlySchedule(days);
        }
    }
}
