using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class SelectedTeamDisplaySnapshot
    {
        internal SelectedTeamDisplaySnapshot(IReadOnlyList<SelectedApplicantDisplay> members, SkillSet displayedSum)
        {
            var copy = new SelectedApplicantDisplay[members.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = members[i];
            Members = Array.AsReadOnly(copy);
            DisplayedSum = displayedSum;
        }

        public int Count => Members.Count;
        public IReadOnlyList<SelectedApplicantDisplay> Members { get; }
        public SkillSet DisplayedSum { get; }
    }

    public sealed class SelectedApplicantDisplay
    {
        internal SelectedApplicantDisplay(int joinOrder, string characterId, string name,
            JobRole primaryRole, SkillSet displayedSkills)
        {
            JoinOrder = joinOrder;
            CharacterId = characterId;
            Name = name;
            PrimaryRole = primaryRole;
            DisplayedSkills = displayedSkills;
        }

        public int JoinOrder { get; }
        public string CharacterId { get; }
        public string Name { get; }
        public JobRole PrimaryRole { get; }
        public SkillSet DisplayedSkills { get; }
    }

    internal static class SelectedTeamDisplayProjection
    {
        internal static SelectedTeamDisplaySnapshot Project(SelectedTeamSnapshot snapshot)
        {
            var members = new SelectedApplicantDisplay[snapshot.Count];
            SkillSet displayedSum = SkillSet.Zero;
            for (int i = 0; i < members.Length; i++)
            {
                var member = snapshot.Members[i];
                members[i] = new SelectedApplicantDisplay(member.JoinOrder, member.CharacterId,
                    member.Name, member.PrimaryRole, member.DisplayedSkills);
                displayedSum = displayedSum.Add(member.DisplayedSkills);
            }
            return new SelectedTeamDisplaySnapshot(members, displayedSum);
        }
    }
}