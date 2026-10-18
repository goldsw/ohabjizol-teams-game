using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    internal sealed class SelectedTeamSnapshot
    {
        internal SelectedTeamSnapshot(IReadOnlyList<SelectedApplicantSnapshot> members)
        {
            var copy = new SelectedApplicantSnapshot[members.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = members[i];
            Members = Array.AsReadOnly(copy);
        }

        internal IReadOnlyList<SelectedApplicantSnapshot> Members { get; }
        internal int Count => Members.Count;
    }

    // Extend typed captured values here when applicant traits have a defined schema.
    // Never retain Applicant or TeamMember references in the captured values.
    internal sealed class SelectedApplicantSnapshot
    {
        internal SelectedApplicantSnapshot(int joinOrder, string characterId, string name,
            JobRole primaryRole, SkillSet actualSkills, SkillSet displayedSkills,
            ApplicantKind kind, MonthlySchedule unavailableSchedule,
            PortfolioDisplayState truePortfolioQuality = PortfolioDisplayState.Normal,
            PortfolioDisplayState? fakePortfolioQuality = null)
        {
            JoinOrder = joinOrder;
            CharacterId = characterId;
            Name = name;
            PrimaryRole = primaryRole;
            // SkillSet is sealed and exposes only immutable integer properties.
            ActualSkills = actualSkills;
            DisplayedSkills = displayedSkills;
            Kind = kind;
            TruePortfolioQuality = truePortfolioQuality;
            FakePortfolioQuality = fakePortfolioQuality;
            _unavailableSchedule = SelectedScheduleSnapshot.Copy(unavailableSchedule);
        }

        internal int JoinOrder { get; }
        internal string CharacterId { get; }
        internal string Name { get; }
        internal JobRole PrimaryRole { get; }
        internal SkillSet ActualSkills { get; }
        internal SkillSet DisplayedSkills { get; }
        internal ApplicantKind Kind { get; }
        internal PortfolioDisplayState TruePortfolioQuality { get; }
        internal PortfolioDisplayState? FakePortfolioQuality { get; }

        readonly MonthlySchedule _unavailableSchedule;
        internal MonthlySchedule UnavailableSchedule => SelectedScheduleSnapshot.Copy(_unavailableSchedule);
    }

    internal static class SelectedScheduleSnapshot
    {
        internal static MonthlySchedule Copy(MonthlySchedule schedule)
        {
            var days = new bool[30];
            for (int day = 1; day <= 30; day++)
                days[day - 1] = schedule.IsMarked(day);
            return new MonthlySchedule(days);
        }
    }
}