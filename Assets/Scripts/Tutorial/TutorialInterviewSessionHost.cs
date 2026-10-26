using System;
using Ohabjizol.Interview;

namespace Ohabjizol.Tutorial
{
    public sealed class TutorialInterviewSessionHost : IInterviewSessionHost
    {
        readonly ApplicantProfileAsset _profile;
        readonly int _seed;
        readonly int _productionDays;
        public TutorialInterviewSessionHost(ApplicantProfileAsset profile, int seed, int productionDays)
        {
            _profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            _seed = seed;
            _productionDays = Math.Max(1, productionDays);
        }
        public bool AllowDecisions => true;
        public bool AllowResultHandoff => false;
        public StageManager CreateStage(InterviewViewReferences views, float fakePortfolioChance = .5f)
        {
            var random = new Random(_seed);
            var sampled = _profile.Sample(random);
            // The lesson compares an honest, normal portfolio with its matching answer.
            // Construct a sandbox copy without changing the shared applicant profile.
            var applicant = new Applicant(sampled.Name, sampled.PrimaryRole, sampled.DisplayedSkills,
                sampled.DisplayedSkills, sampled.Documents, ApplicantKind.Normal,
                PortfolioDisplayState.Normal, characterId: sampled.CharacterId);
            var definition = new StageDefinition(StageId.EntranceGameJam, "튜토리얼",
                StageCatalog.Get(StageId.EntranceGameJam).Theme, new[] { applicant });
            var context = new StageContext("tutorial-" + Guid.NewGuid().ToString("N"), definition);
            return new StageManager(context, (runId, result) => { }, _productionDays, random, 0f);
        }
    }
}
