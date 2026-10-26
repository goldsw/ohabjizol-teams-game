using NUnit.Framework;

namespace Ohabjizol.Interview
{
    public sealed class CampaignEndingStarRuleTests
    {
        [TestCase(0, 0, EndingKind.Bad)]
        [TestCase(40, 4, EndingKind.Bad)]
        [TestCase(49, 4, EndingKind.Bad)]
        [TestCase(50, 5, EndingKind.Normal)]
        [TestCase(59, 5, EndingKind.Normal)]
        [TestCase(60, 6, EndingKind.Normal)]
        [TestCase(69, 6, EndingKind.Normal)]
        [TestCase(70, 7, EndingKind.Good)]
        [TestCase(79, 7, EndingKind.Good)]
        [TestCase(84, 8, EndingKind.Good)]
        [TestCase(85, 8, EndingKind.Good)]
        [TestCase(100, 10, EndingKind.Good)]
        public void EndingMatchesVisibleHalfStars(int accuracy, int halfStars, EndingKind expected)
        {
            var run = new CampaignRunProgress("star-ending-" + accuracy);
            foreach (var stage in StageExecutionPolicy.ActiveStages)
                run.Record(stage, accuracy);

            Assert.IsTrue(run.IsComplete);
            Assert.AreEqual(halfStars * StageExecutionPolicy.ActiveStages.Count, run.TotalHalfStars);
            Assert.AreEqual(expected, run.Ending);
        }
    }
}
