using System;

namespace Ohabjizol.Interview
{
    public static class FulfillmentRate
    {
        // PROTOTYPE ONLY
        // 최종 기획 확정 후 교체 예정

        // PROTOTYPE RULE
        // Final verification scoring is not yet defined.
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
                return 0;

            double average = total / counted;
            return (int)Math.Round(average * 100d, MidpointRounding.AwayFromZero);
        }

        static void Include(int actual, int required, ref double total, ref int counted)
        {
            if (required <= 0)
                return;

            double ratio = actual / (double)required;
            if (ratio > 1d)
                ratio = 1d;
            if (ratio < 0d)
                ratio = 0d;

            total += ratio;
            counted++;
        }
    }
}
