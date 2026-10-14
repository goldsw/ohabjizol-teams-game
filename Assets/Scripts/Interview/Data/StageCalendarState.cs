using System;
using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public sealed class StageCalendarState
    {
        readonly bool[] _production = new bool[31];
        public StageCalendarState(int productionDays = 15)
        {
            if (productionDays < 1 || productionDays > 30) throw new ArgumentOutOfRangeException(nameof(productionDays));
            ProductionDays = productionDays;
            var days = new int[30];
            for (int i = 0; i < days.Length; i++) days[i] = i + 1;
            var random = new Random();
            for (int i = 0; i < productionDays; i++) {
                int selected = random.Next(i, days.Length);
                int temp = days[i]; days[i] = days[selected]; days[selected] = temp;
            }
            var dates = new int[productionDays];
            Array.Copy(days, dates, productionDays);
            Array.Sort(dates);
            foreach (int date in dates) _production[date] = true;
            DatesReadonly = Array.AsReadOnly(dates);
        }
        public int ProductionDays { get; }
        public IReadOnlyList<int> DatesReadonly { get; }
        public IReadOnlyList<int> ProductionDates => DatesReadonly;
        public bool IsProductionDay(int day)
        {
            if (day < 1 || day > 30) throw new ArgumentOutOfRangeException(nameof(day));
            return _production[day];
        }
    }
}