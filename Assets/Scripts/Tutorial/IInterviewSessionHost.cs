using Ohabjizol.Interview;

namespace Ohabjizol.Tutorial
{
    public interface IInterviewSessionHost
    {
        StageManager CreateStage(InterviewViewReferences views);
        bool AllowDecisions { get; }
        bool AllowResultHandoff { get; }
    }
}
