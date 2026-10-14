using System;
namespace Ohabjizol.Interview
{
    public static class StarRatingRule
    {
        public static int HalfUnits(int percent)
        {
            return (Math.Max(0, Math.Min(100, percent)) + 5) / 10;
        }
    }
}
