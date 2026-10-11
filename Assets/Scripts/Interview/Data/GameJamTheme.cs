using System;

namespace Ohabjizol.Interview
{
    public sealed class GameJamTheme
    {
        public GameJamTheme(string displayName, SkillSet requiredSkills, JobRole coreRole)
        {
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("A theme needs a display name.", nameof(displayName));
            if (requiredSkills == null)
                throw new ArgumentNullException(nameof(requiredSkills));

            DisplayName = displayName;
            RequiredSkills = requiredSkills;
            CoreRole = coreRole;
        }

        public string DisplayName { get; }

        public SkillSet RequiredSkills { get; }

        public JobRole CoreRole { get; }
    }
}
