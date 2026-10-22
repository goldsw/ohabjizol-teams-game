using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class SelectedTeamResultMember
    {
        public SelectedTeamResultMember(string name, JobRole role, bool hasFalsePortfolio)
            : this(name, role, hasFalsePortfolio, null, new MonthlySchedule(new bool[30])) { }

        public SelectedTeamResultMember(string name, JobRole role, bool hasFalsePortfolio,
            string characterId, MonthlySchedule unavailableSchedule)
        {
            Name = name;
            Role = role;
            HasFalsePortfolio = hasFalsePortfolio;
            CharacterId = characterId;
            _unavailableSchedule = SelectedScheduleSnapshot.Copy(unavailableSchedule);
        }

        public string Name { get; }
        public JobRole Role { get; }
        public bool HasFalsePortfolio { get; }
        public string CharacterId { get; }
        readonly MonthlySchedule _unavailableSchedule;
        public MonthlySchedule UnavailableSchedule => SelectedScheduleSnapshot.Copy(_unavailableSchedule);
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
                result[i] = new SelectedTeamResultMember(member.Name, member.PrimaryRole, member.HasFalsePortfolio,
                    member.CharacterId, member.UnavailableSchedule);
            }
            return Array.AsReadOnly(result);
        }
    }
}
