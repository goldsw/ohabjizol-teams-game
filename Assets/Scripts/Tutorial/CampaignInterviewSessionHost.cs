using Ohabjizol.Interview;

namespace Ohabjizol.Tutorial
{
    public sealed class CampaignInterviewSessionHost : IInterviewSessionHost
    {
        public bool AllowDecisions => true;
        public bool AllowResultHandoff => true;
        public StageManager CreateStage(InterviewViewReferences views)
        {
            StageCompletionService.RecoverPending();
            string runId = CampaignRunStore.Load().RunId;
            StageDefinition definition = StageCatalog.Get(StageSelection.Current);
            int days = views.ScheduleConfig != null ? views.ScheduleConfig.GetProductionDays(definition.Id) : 15;
            return new StageManager(new StageContext(runId, definition), StageCompletionService.Complete, days);
        }
    }
}
