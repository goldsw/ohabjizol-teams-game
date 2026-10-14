using System;

namespace Ohabjizol.Interview
{
    [Serializable]
    public sealed class DocumentInitialState
    {
        public bool applicationOpen = true;
        public bool portfolioOpen;
        public bool scheduleOpen;
        public bool interviewRecordOpen;

        public bool IsOpen(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Application: return applicationOpen;
                case DocumentKind.Portfolio: return portfolioOpen;
                case DocumentKind.Schedule: return scheduleOpen;
                case DocumentKind.InterviewRecord: return interviewRecordOpen;
                default: return false;
            }
        }
    }
}
