using System;

namespace Ohabjizol.Interview
{
    public static class SchedulePenaltyRule
    {
        public static double Apply(double baseAccuracyPercent, InterviewScheduleResult schedule)
        {
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));
            int missedWorkDays = 0;
            foreach (var participant in schedule.Participants)
                missedWorkDays += participant.ConflictDays.Count;

            // Each of six team slots contributes an equal share; all deductions use the original score.
            double deduction = (baseAccuracyPercent / 6d) * (missedWorkDays / 30d);
            return Math.Max(0d, baseAccuracyPercent - deduction);
        }
    }
}
