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
            ResultGrade grade)
        {
            if (actualSkills == null)
                throw new ArgumentNullException(nameof(actualSkills));

            ActualSkills = actualSkills;
            FulfillmentPercent = fulfillmentPercent;
            VerificationSuccessCount = verificationSuccessCount;
            VerificationFailureCount = verificationFailureCount;
            FraudCount = fraudCount;
            Grade = grade;
        }

        public SkillSet ActualSkills { get; }

        public int FulfillmentPercent { get; }

        public int VerificationSuccessCount { get; }

        public int VerificationFailureCount { get; }

        public int FraudCount { get; }

        public ResultGrade Grade { get; }
    }
}
