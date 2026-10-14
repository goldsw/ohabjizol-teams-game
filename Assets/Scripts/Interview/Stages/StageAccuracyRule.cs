using System;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Accuracy against every required skill above zero. Overshoot and shortfall both count.
    /// </summary>
    public static class StageAccuracyRule
    {
        public const int ClearPercent = 70;

        public static int Percent(SkillSet actual, SkillSet required)
        {
            if (actual == null)
                throw new ArgumentNullException(nameof(actual));
            if (required == null)
                throw new ArgumentNullException(nameof(required));

            double total = 0d;
            int counted = 0;
            Include(actual.Planning, required.Planning, ref total, ref counted);
            Include(actual.Programming, required.Programming, ref total, ref counted);
            Include(actual.Graphics, required.Graphics, ref total, ref counted);
            if (counted == 0)
                return 100;

            double average = total / counted;
            return (int)Math.Round(100d * (1d - average), MidpointRounding.AwayFromZero);
        }

        public static bool IsClear(int percent)
        {
            return percent >= ClearPercent;
        }

        static void Include(int actual, int required, ref double total, ref int counted)
        {
            if (required <= 0)
                return;

            double error = Math.Abs(actual - required) / (double)required;
            if (error > 1d)
                error = 1d;

            total += error;
            counted++;
        }
    }
}
