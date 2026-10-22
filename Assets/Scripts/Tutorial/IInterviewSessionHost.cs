using Ohabjizol.Interview;

namespace Ohabjizol.Tutorial
{
    public interface IInterviewSessionHost
    {
        StageManager CreateStage(InterviewViewReferences views, float fakePortfolioChance = .5f);
        bool AllowDecisions { get; }
        bool AllowResultHandoff { get; }
    }
}
