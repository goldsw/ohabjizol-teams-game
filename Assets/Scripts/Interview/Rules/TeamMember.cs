using System;

namespace Ohabjizol.Interview
{
    public sealed class TeamMember
    {
        public TeamMember(Applicant applicant, int joinOrder)
        {
            if (applicant == null)
                throw new ArgumentNullException(nameof(applicant));
            if (joinOrder <= 0)
                throw new ArgumentOutOfRangeException(nameof(joinOrder), "Join order starts at 1.");

            Applicant = applicant;
            JoinOrder = joinOrder;
        }

        public Applicant Applicant { get; }

        public int JoinOrder { get; }
    }
}
