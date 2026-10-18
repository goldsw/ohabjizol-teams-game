namespace Ohabjizol.Interview
{
    internal static class InterviewScoreProcessor
    {
        internal static InterviewScoreSummary Process(SelectedTeamSnapshot snapshot, GameJamTheme theme)
        {
            SkillSet actual = SkillSet.Zero;
            int fraudCount = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var member = snapshot.Members[i];
                actual = actual.Add(member.ActualSkills);
                if (member.Kind == ApplicantKind.Fraud)
                    fraudCount++;
            }

            SkillSet required = theme.RequiredSkills;
            double rawAccuracyPercent = StageAccuracyRule.RawPercent(actual, required);
            int accuracyPercent = StageAccuracyRule.FloorPercent(rawAccuracyPercent);
            int fulfillmentPercent = FulfillmentRate.Percent(actual, required);
            ResultGrade grade = ResultGradeRule.Evaluate(actual, required, theme.CoreRole, fraudCount);
            bool isClear = StageAccuracyRule.IsClear(accuracyPercent);
            return new InterviewScoreSummary(actual, required, fraudCount, fulfillmentPercent,
                grade, accuracyPercent, isClear, rawAccuracyPercent);
        }
    }

    internal sealed class InterviewScoreSummary
    {
        internal InterviewScoreSummary(SkillSet actualSum, SkillSet requiredSkills, int fraudCount,
            int fulfillmentPercent, ResultGrade grade, int accuracyPercent, bool isClear, double rawAccuracyPercent)
        {
            ActualSum = actualSum;
            RequiredSkills = requiredSkills;
            FraudCount = fraudCount;
            FulfillmentPercent = fulfillmentPercent;
            Grade = grade;
            AccuracyPercent = accuracyPercent;
            RawAccuracyPercent = rawAccuracyPercent;
            IsClear = isClear;
        }

        internal SkillSet ActualSum { get; }
        internal SkillSet RequiredSkills { get; }
        internal int FraudCount { get; }
        internal int FulfillmentPercent { get; }
        internal ResultGrade Grade { get; }
        internal int AccuracyPercent { get; }
        internal double RawAccuracyPercent { get; }
        internal bool IsClear { get; }
    }
}