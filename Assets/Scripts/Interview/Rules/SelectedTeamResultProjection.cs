using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class SelectedTeamResultMember
    {
        public SelectedTeamResultMember(string name, JobRole role, bool hasFalsePortfolio)
        {
            Name = name;
            Role = role;
            HasFalsePortfolio = hasFalsePortfolio;
        }

        public string Name { get; }
        public JobRole Role { get; }
        public bool HasFalsePortfolio { get; }
    }

    internal static class SelectedTeamResultProjection
    {
        internal static IReadOnlyList<SelectedTeamResultMember> Project(SelectedTeamSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var ordered = new SelectedApplicantSnapshot[snapshot.Count];
            for (int i = 0; i < ordered.Length; i++)
                ordered[i] = snapshot.Members[i];
            Array.Sort(ordered, (left, right) => left.JoinOrder.CompareTo(right.JoinOrder));

            var result = new SelectedTeamResultMember[ordered.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var member = ordered[i];
                bool hasFalsePortfolio = member.FakePortfolioQuality.HasValue
                    && member.FakePortfolioQuality.Value != member.TruePortfolioQuality;
                result[i] = new SelectedTeamResultMember(member.Name, member.PrimaryRole, hasFalsePortfolio);
            }
            return Array.AsReadOnly(result);
        }
    }
}