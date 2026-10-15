using System;
using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public sealed class StageCalendarState
    {
        public StageCalendarState(int productionDays = 15) : this(CreateSchedule(productionDays)) { }
        public StageCalendarState(MonthlySchedule projectSchedule)
        {
            ProjectSchedule = projectSchedule;
            DatesReadonly = Array.AsReadOnly(projectSchedule.GetMarkedDays());
        }
        static MonthlySchedule CreateSchedule(int productionDays)
        {
            if (productionDays < 1 || productionDays > 30) throw new ArgumentOutOfRangeException(nameof(productionDays));
            return MonthlySchedule.CreateRandom(productionDays, new Random());
        }
        public MonthlySchedule ProjectSchedule { get; }
        public int ProductionDays => ProjectSchedule.Count;
        public IReadOnlyList<int> DatesReadonly { get; }
        public IReadOnlyList<int> ProductionDates => DatesReadonly;
        public bool IsProductionDay(int day) => ProjectSchedule.IsMarked(day);
    }
}
