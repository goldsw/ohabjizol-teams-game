using System;
namespace Ohabjizol.Interview
{
    public sealed class ApplicantDocumentPacket
    {
        public ApplicantDocumentPacket(string applicantKey, string displayName, JobRole role,
            string applicationText, string portfolioText, PortfolioDisplayState displayState,
            SkillSet displayedSkills = null, string characterId = null)
        {
            if (string.IsNullOrWhiteSpace(applicantKey)) throw new ArgumentException("An applicant needs a key.", nameof(applicantKey));
            if (!Enum.IsDefined(typeof(JobRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
            if (!Enum.IsDefined(typeof(PortfolioDisplayState), displayState)) throw new ArgumentOutOfRangeException(nameof(displayState));
            ApplicantKey = applicantKey;
            DisplayName = displayName ?? string.Empty;
            Role = role;
            ApplicationText = applicationText ?? string.Empty;
            PortfolioText = portfolioText ?? string.Empty;
            DisplayState = displayState;
            DisplayedSkills = displayedSkills;
            CharacterId = characterId ?? string.Empty;
        }
        public string ApplicantKey { get; }
        public string CharacterId { get; }
        public string DisplayName { get; }
        public JobRole Role { get; }
        public string ApplicationText { get; }
        public string PortfolioText { get; }
        public PortfolioDisplayState DisplayState { get; }
        public SkillSet DisplayedSkills { get; }
        public PortfolioDisplayState TruePortfolioQuality => DisplayState;
    }
}
