using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class StageDefinition
    {
        public StageDefinition(StageId id, string displayName, GameJamTheme theme, IReadOnlyList<Applicant> applicants)
        {
            if (!Enum.IsDefined(typeof(StageId), id))
                throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("A stage needs a display name.", nameof(displayName));
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));
            if (applicants == null)
                throw new ArgumentNullException(nameof(applicants));

            var copy = new Applicant[applicants.Count];
            for (int i = 0; i < applicants.Count; i++)
                copy[i] = applicants[i];

            Id = id;
            DisplayName = displayName;
            Theme = theme;
            Applicants = copy;
        }

        public StageId Id { get; }

        public string DisplayName { get; }

        public GameJamTheme Theme { get; }

        public IReadOnlyList<Applicant> Applicants { get; }
    }
}
