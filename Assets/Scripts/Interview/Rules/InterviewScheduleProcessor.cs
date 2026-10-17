namespace Ohabjizol.Interview
{
    internal static class InterviewScheduleProcessor
    {
        internal static InterviewScheduleResult Process(SelectedTeamSnapshot snapshot, MonthlySchedule projectSchedule)
        {
            var project = SelectedScheduleSnapshot.Copy(projectSchedule);
            var participants = new ParticipantScheduleResult[snapshot.Count];
            for (int i = 0; i < participants.Length; i++)
            {
                var member = snapshot.Members[i];
                participants[i] = new ParticipantScheduleResult(member.Name,
                    SelectedScheduleSnapshot.Copy(member.UnavailableSchedule), project);
            }
            return new InterviewScheduleResult(project, participants);
        }
    }
}