using Ohabjizol.Interview;

namespace Ohabjizol.Tutorial
{
    public sealed class CampaignInterviewSessionHost : IInterviewSessionHost
    {
        public bool AllowDecisions => true;
        public bool AllowResultHandoff => true;
        public StageManager CreateStage(InterviewViewReferences views, float fakePortfolioChance = .5f)
        {
            StageCompletionService.RecoverPending();
            var run = CampaignRunStore.Load();
#if UNITY_EDITOR
            // Direct scene Play has no menu selection. A completed run cannot accept a new score.
            if (!StageSelection.HasValue)
            {
                if (run.IsComplete) run = CampaignRunStore.StartNewRun();
                StageSelection.Set(run.NextStage);
            }
#endif
            string runId = run.RunId;
            StageDefinition definition = GameThemeCatalog.WithRunTheme(StageCatalog.Get(StageSelection.Current), runId);
            int days = views.ScheduleConfig != null ? views.ScheduleConfig.GetProductionDays(definition.Id) : 15;
            return new StageManager(new StageContext(runId, definition), StageCompletionService.Complete,
                days, fakePortfolioChance: fakePortfolioChance);
        }
    }
}
