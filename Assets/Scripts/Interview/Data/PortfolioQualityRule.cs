using System;

namespace Ohabjizol.Interview
{
    public static class PortfolioQualityRule
    {
        // Semantic order is explicit; enum numeric values are not ranks.
        public static int Rank(PortfolioDisplayState quality)
        {
            switch (quality)
            {
                case PortfolioDisplayState.Bad: return 0;
                case PortfolioDisplayState.Normal: return 1;
                case PortfolioDisplayState.Good: return 2;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }
        public static PortfolioDisplayState? CreateFake(PortfolioDisplayState actual, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            switch (actual)
            {
                case PortfolioDisplayState.Bad: return null;
                case PortfolioDisplayState.Normal: return PortfolioDisplayState.Bad;
                case PortfolioDisplayState.Good:
                    return random.Next(2) == 0 ? PortfolioDisplayState.Normal : PortfolioDisplayState.Bad;
                default: throw new ArgumentOutOfRangeException(nameof(actual));
            }
        }
    }
}
