using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// One applicant's facts. This type does not judge them.
    /// </summary>
    public sealed class Applicant
    {
        public Applicant(
            string name,
            JobRole primaryRole,
            SkillSet displayedSkills,
            SkillSet actualSkills,
            IReadOnlyList<ApplicantDocument> documents,
            ApplicantKind kind, PortfolioDisplayState truePortfolioQuality = PortfolioDisplayState.Normal,
            PortfolioDisplayState? fakePortfolioQuality = null,
            MonthlySchedule unavailableSchedule = default(MonthlySchedule))
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("An applicant needs a name.", nameof(name));
            if (displayedSkills == null)
                throw new ArgumentNullException(nameof(displayedSkills));
            if (actualSkills == null)
                throw new ArgumentNullException(nameof(actualSkills));
            if (documents == null || documents.Count != 4)
                throw new ArgumentException("An applicant has four documents.", nameof(documents));

            if (!Enum.IsDefined(typeof(PortfolioDisplayState), truePortfolioQuality))
                throw new ArgumentOutOfRangeException(nameof(truePortfolioQuality));
            if (fakePortfolioQuality.HasValue && (!Enum.IsDefined(typeof(PortfolioDisplayState), fakePortfolioQuality.Value) || PortfolioQualityRule.Rank(fakePortfolioQuality.Value) >= PortfolioQualityRule.Rank(truePortfolioQuality)))
                throw new ArgumentException("Fake quality must be strictly worse than true quality.", nameof(fakePortfolioQuality));
            UnavailableSchedule = unavailableSchedule;
            TruePortfolioQuality = truePortfolioQuality;
            FakePortfolioQuality = fakePortfolioQuality;
            Name = name;
            PrimaryRole = primaryRole;
            DisplayedSkills = displayedSkills;
            ActualSkills = actualSkills;
            Documents = new ApplicantDocument[]
            {
                documents[0],
                documents[1],
                documents[2],
                documents[3]
            };
            Kind = kind;
        }

        public string Name { get; }

        public JobRole PrimaryRole { get; }

        public SkillSet DisplayedSkills { get; }

        public SkillSet ActualSkills { get; }

        public IReadOnlyList<ApplicantDocument> Documents { get; }

        public MonthlySchedule UnavailableSchedule { get; }

        public ApplicantKind Kind { get; }
        public PortfolioDisplayState TruePortfolioQuality { get; }
        public PortfolioDisplayState? FakePortfolioQuality { get; }
    }
}
