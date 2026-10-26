using NUnit.Framework;

namespace Ohabjizol.Interview
{
    public sealed class SchedulePenaltyRuleTests
    {
        [TestCase(90d, 0, 1, 90d)]
        [TestCase(90d, 6, 1, 87d)]
        [TestCase(90d, 6, 2, 84d)]
        [TestCase(90d, 30, 1, 75d)]
        [TestCase(90d, 30, 6, 0d)]
        [TestCase(0d, 6, 6, 0d)]
        [TestCase(100d, 1, 1, 99.44444444444444d)]
        public void DeductsEachParticipantsShareFromOriginalScore(double score, int days,
            int participantCount, double expected)
        {
            var workDays = new bool[30];
            var missedDays = new bool[30];
            for (int i = 0; i < 30; i++) workDays[i] = true;
            for (int i = 0; i < days; i++) missedDays[i] = true;
            var project = new MonthlySchedule(workDays);
            var participants = new ParticipantScheduleResult[participantCount];
            for (int i = 0; i < participants.Length; i++)
                participants[i] = new ParticipantScheduleResult("Applicant " + i,
                    new MonthlySchedule(missedDays), project);

            double actual = SchedulePenaltyRule.Apply(score, new InterviewScheduleResult(project, participants));
            Assert.That(actual, Is.EqualTo(expected).Within(1e-10));
        }

        [Test]
        public void UnavailableNonWorkDaysDoNotReduceScore()
        {
            var workDays = new bool[30];
            var restDays = new bool[30];
            workDays[0] = true;
            restDays[1] = true;
            var project = new MonthlySchedule(workDays);
            var participant = new ParticipantScheduleResult("Applicant", new MonthlySchedule(restDays), project);
            var schedule = new InterviewScheduleResult(project, new[] { participant });

            Assert.AreEqual(90d, SchedulePenaltyRule.Apply(90d, schedule));
        }
    }
}
