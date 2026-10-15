using System;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Values for a result screen. This type does not calculate them.
    /// </summary>
    public sealed class InterviewResult
    {
        public InterviewResult(
            SkillSet actualSkills,
            int fulfillmentPercent,
            int verificationSuccessCount,
            int verificationFailureCount,
            int fraudCount,
            ResultGrade grade,
            StageId stage,
            SkillSet requiredSkills,
            int accuracyPercent,
            bool isCleared,
            InterviewScheduleResult scheduleResult = null)
        {
            if (actualSkills == null)
                throw new ArgumentNullException(nameof(actualSkills));
            if (requiredSkills == null)
                throw new ArgumentNullException(nameof(requiredSkills));

            ActualSkills = actualSkills;
            FulfillmentPercent = fulfillmentPercent;
            VerificationSuccessCount = verificationSuccessCount;
            VerificationFailureCount = verificationFailureCount;
            FraudCount = fraudCount;
            Grade = grade;
            Stage = stage;
            RequiredSkills = requiredSkills;
            AccuracyPercent = accuracyPercent;
            IsCleared = isCleared;
            ScheduleResult = scheduleResult ?? new InterviewScheduleResult(default(MonthlySchedule), Array.Empty<ParticipantScheduleResult>());
        }

        public SkillSet ActualSkills { get; }

        public int FulfillmentPercent { get; }

        public int VerificationSuccessCount { get; }

        public int VerificationFailureCount { get; }

        public int FraudCount { get; }

        public ResultGrade Grade { get; }

        public StageId Stage { get; }

        public SkillSet RequiredSkills { get; }

        public int AccuracyPercent { get; }

        public bool IsCleared { get; }

        public InterviewScheduleResult ScheduleResult { get; }
    }
}
