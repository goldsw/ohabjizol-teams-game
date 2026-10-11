using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Up to four members. Displayed and actual skill sums stay separate.
    /// </summary>
    public sealed class TeamRoster
    {
        public const int MaxMembers = 4;

        readonly List<TeamMember> _members = new List<TeamMember>();
        readonly ReadOnlyCollection<TeamMember> _visibleMembers;
        SkillSet _displayedSum = SkillSet.Zero;
        SkillSet _actualSum = SkillSet.Zero;

        public TeamRoster()
        {
            _visibleMembers = _members.AsReadOnly();
        }

        public IReadOnlyList<TeamMember> Members
        {
            get { return _visibleMembers; }
        }

        public int Count
        {
            get { return _members.Count; }
        }

        public SkillSet DisplayedSum
        {
            get { return _displayedSum; }
        }

        public SkillSet ActualSum
        {
            get { return _actualSum; }
        }

        public int FraudCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _members.Count; i++)
                {
                    if (_members[i].Applicant.Kind == ApplicantKind.Fraud)
                        count++;
                }

                return count;
            }
        }

        internal bool TryAdd(Applicant applicant)
        {
            if (applicant == null)
                throw new ArgumentNullException(nameof(applicant));
            if (_members.Count >= MaxMembers)
                return false;

            _members.Add(new TeamMember(applicant, _members.Count + 1));
            _displayedSum = _displayedSum.Add(applicant.DisplayedSkills);
            _actualSum = _actualSum.Add(applicant.ActualSkills);
            return true;
        }
    }
}
