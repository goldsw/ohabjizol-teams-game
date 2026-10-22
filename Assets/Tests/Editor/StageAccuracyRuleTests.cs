using NUnit.Framework;

namespace Ohabjizol.Interview
{
    public sealed class StageAccuracyRuleTests
    {
        [Test]
        public void NearRequirementFloorsTo86AndClears()
        {
            int percent = StageAccuracyRule.Percent(new SkillSet(12, 5, 12), new SkillSet(10, 5, 15));
            Assert.AreEqual(86, percent);
            Assert.IsTrue(StageAccuracyRule.IsClear(percent));
        }

        [Test]
        public void EmptyTeamAgainstRequirementIsZero()
        {
            int percent = StageAccuracyRule.Percent(new SkillSet(0, 0, 0), new SkillSet(10, 5, 15));
            Assert.AreEqual(0, percent);
            Assert.IsFalse(StageAccuracyRule.IsClear(percent));
        }

        [Test]
        public void NoRequiredSkillIsPerfect()
        {
            int percent = StageAccuracyRule.Percent(new SkillSet(1, 2, 3), new SkillSet(0, 0, 0));
            Assert.AreEqual(100, percent);
        }

        [Test]
        public void MoreThanDoubleRequirementCapsSkillErrorAndFloorsTo66()
        {
            int percent = StageAccuracyRule.Percent(new SkillSet(30, 5, 15), new SkillSet(10, 5, 15));
            Assert.AreEqual(66, percent);
        }

        [Test]
        public void HalfwayBelow70FloorsTo69AndDoesNotClear()
        {
            int percent = StageAccuracyRule.Percent(new SkillSet(139, 0, 0), new SkillSet(200, 0, 0));
            Assert.AreEqual(69, percent);
            Assert.IsFalse(StageAccuracyRule.IsClear(percent));
        }

        [Test]
        public void RecordKeepsClearAndBestAccuracy()
        {
            var progress = new StageProgress();
            Assert.AreEqual(-1, progress.BestAccuracy(StageId.EntranceGameJam));
            Assert.IsTrue(progress.IsPlayable(StageId.EntranceGameJam));
            Assert.IsFalse(progress.IsPlayable(StageId.GameProgrammingIntro));

            progress.Record(StageId.EntranceGameJam, 69);
            Assert.IsFalse(progress.IsCleared(StageId.EntranceGameJam));
            Assert.AreEqual(StageId.EntranceGameJam, progress.NextStage);

            progress.Record(StageId.EntranceGameJam, 70);
            Assert.IsTrue(progress.IsCleared(StageId.EntranceGameJam));
            Assert.IsTrue(progress.IsPlayable(StageId.GameProgrammingIntro));
            Assert.AreEqual(StageId.GameProgrammingIntro, progress.NextStage);

            progress.Record(StageId.EntranceGameJam, 40);
            Assert.IsTrue(progress.IsCleared(StageId.EntranceGameJam));
            Assert.AreEqual(70, progress.BestAccuracy(StageId.EntranceGameJam));

            progress.Record(StageId.GameProgrammingIntro, 80);
            progress.Record(StageId.SecondYear, 90);
            progress.Record(StageId.ThirdYear, 91);
            Assert.IsTrue(progress.AllCleared);
            Assert.AreEqual(StageId.ThirdYear, progress.NextStage);
        }
    }
}
