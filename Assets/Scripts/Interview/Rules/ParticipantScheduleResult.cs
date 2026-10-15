using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class ParticipantScheduleResult
    {
        public ParticipantScheduleResult(string applicantName, MonthlySchedule unavailableSchedule, MonthlySchedule projectSchedule)
        {
            if (string.IsNullOrEmpty(applicantName)) throw new ArgumentException("An applicant needs a name.", nameof(applicantName));
            ApplicantName = applicantName;
            UnavailableSchedule = unavailableSchedule;
            var conflicts = new List<int>();
            for (int day = 1; day <= 30; day++)
                if (projectSchedule.IsMarked(day) && unavailableSchedule.IsMarked(day)) conflicts.Add(day);
            ConflictDays = conflicts.AsReadOnly();
        }
        public string ApplicantName { get; }
        public MonthlySchedule UnavailableSchedule { get; }
        public IReadOnlyList<int> ConflictDays { get; }
    }
}
