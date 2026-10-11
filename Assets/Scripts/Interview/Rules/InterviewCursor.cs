namespace Ohabjizol.Interview
{
    /// <summary>
    /// Forward-only applicant index. Four accepts or the end of the list finishes it.
    /// </summary>
    public sealed class InterviewCursor
    {
        public int Index { get; private set; }

        public bool IsFinished { get; private set; }

        public void Advance(int applicantCount, int acceptedCount)
        {
            if (IsFinished)
                return;

            if (acceptedCount >= TeamRoster.MaxMembers || Index + 1 >= applicantCount)
            {
                IsFinished = true;
                return;
            }

            Index++;
        }
    }
}
