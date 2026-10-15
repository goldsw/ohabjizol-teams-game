using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class InterviewScheduleResult
    {
        public InterviewScheduleResult(MonthlySchedule projectSchedule, IReadOnlyList<ParticipantScheduleResult> participants)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            ProjectSchedule = projectSchedule;
            var copy = new ParticipantScheduleResult[participants.Count];
            var conflicting = new bool[30];
            for (int i = 0; i < participants.Count; i++)
            {
                var participant = participants[i] ?? throw new ArgumentException("A participant cannot be null.", nameof(participants));
                copy[i] = new ParticipantScheduleResult(participant.ApplicantName, participant.UnavailableSchedule, projectSchedule);
                foreach (int day in copy[i].ConflictDays) conflicting[day - 1] = true;
            }
            Participants = Array.AsReadOnly(copy);
            ConflictingDays = Array.AsReadOnly(new MonthlySchedule(conflicting).GetMarkedDays());
        }
        public MonthlySchedule ProjectSchedule { get; }
        public IReadOnlyList<ParticipantScheduleResult> Participants { get; }
        public IReadOnlyList<int> ConflictingDays { get; }
    }
}
