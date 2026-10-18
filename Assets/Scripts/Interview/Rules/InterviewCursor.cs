namespace Ohabjizol.Interview
{
    /// <summary>
    /// Forward-only applicant index. Reaching team capacity or the end of the list finishes it.
    /// </summary>
    public sealed class InterviewCursor
    {
        public int Index { get; private set; }

        public bool IsFinished { get; private set; }

        public void Advance(int applicantCount, int acceptedCount)
            => Advance(applicantCount, acceptedCount, TeamRoster.MaxMembers);

        public void Advance(int applicantCount, int acceptedCount, int teamCapacity)
        {
            if (teamCapacity <= 0) throw new System.ArgumentOutOfRangeException(nameof(teamCapacity));
            if (IsFinished)
                return;

            if (acceptedCount >= teamCapacity || Index + 1 >= applicantCount)
            {
                IsFinished = true;
                return;
            }

            Index++;
        }
    }
}
