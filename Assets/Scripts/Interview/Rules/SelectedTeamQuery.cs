using System;

namespace Ohabjizol.Interview
{
    internal static class SelectedTeamQuery
    {
        internal static SelectedTeamSnapshot Capture(TeamRoster roster)
        {
            if (roster == null)
                throw new ArgumentNullException(nameof(roster));

            var members = roster.Members;
            var captured = new SelectedApplicantSnapshot[members.Count];
            for (int i = 0; i < captured.Length; i++)
            {
                var member = members[i];
                var applicant = member.Applicant;
                captured[i] = new SelectedApplicantSnapshot(member.JoinOrder, applicant.CharacterId,
                    applicant.Name, applicant.PrimaryRole, applicant.ActualSkills,
                    applicant.DisplayedSkills, applicant.Kind, applicant.UnavailableSchedule,
                    applicant.TruePortfolioQuality, applicant.FakePortfolioQuality);
            }
            return new SelectedTeamSnapshot(captured);
        }
    }
}