using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// One interview pass. Grade math stays in <see cref="ResultGradeRule"/>.
    /// </summary>
    public sealed class InterviewSession
    {
        readonly Applicant[] _applicants;
        readonly InterviewCursor _cursor = new InterviewCursor();
        readonly TeamRoster _team = new TeamRoster();
        string _selectedLineId;
        int _verificationSuccessCount;
        int _verificationFailureCount;

        public InterviewSession(GameJamTheme theme, IReadOnlyList<Applicant> applicants)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            Theme = theme;
            _applicants = CopyApplicants(applicants);
            if (_applicants.Length == 0)
            {
                _cursor.Advance(0, 0);
                BuildResult();
            }
        }

        public GameJamTheme Theme { get; }

        public IReadOnlyList<Applicant> Applicants
        {
            get { return new ReadOnlyCollection<Applicant>(_applicants); }
        }

        public TeamRoster Team
        {
            get { return _team; }
        }

        public string SelectedLineId
        {
            get { return _selectedLineId; }
        }

        public int VerificationSuccessCount
        {
            get { return _verificationSuccessCount; }
        }

        public int VerificationFailureCount
        {
            get { return _verificationFailureCount; }
        }

        public int CurrentIndex
        {
            get { return _cursor.Index; }
        }

        public bool IsFinished
        {
            get { return _cursor.IsFinished; }
        }

        public InterviewResult Result { get; private set; }

        public Applicant Current
        {
            get
            {
                if (_cursor.Index < 0 || _cursor.Index >= _applicants.Length)
                    return null;
                return _applicants[_cursor.Index];
            }
        }

        public void SelectLine(string lineId)
        {
            if (_cursor.IsFinished || string.IsNullOrWhiteSpace(lineId))
                return;

            Applicant current = Current;
            if (current == null || !OwnsLine(current, lineId))
                return;

            if (_selectedLineId == lineId)
                _selectedLineId = null;
            else
                _selectedLineId = lineId;
        }

        public void Accept()
        {
            if (_cursor.IsFinished)
                return;

            Applicant current = Current;
            if (current == null)
                return;

            Accumulate(VerificationRule.Evaluate(current, _selectedLineId));
            _team.TryAdd(current);
            _selectedLineId = null;
            _cursor.Advance(_applicants.Length, _team.Count);
            if (_cursor.IsFinished)
                BuildResult();
        }

        public void Reject()
        {
            if (_cursor.IsFinished)
                return;

            Applicant current = Current;
            if (current == null)
                return;

            Accumulate(VerificationRule.Evaluate(current, _selectedLineId));
            _selectedLineId = null;
            _cursor.Advance(_applicants.Length, _team.Count);
            if (_cursor.IsFinished)
                BuildResult();
        }

        void Accumulate(VerificationOutcome outcome)
        {
            if (outcome == VerificationOutcome.Success)
                _verificationSuccessCount++;
            else if (outcome == VerificationOutcome.Failure)
                _verificationFailureCount++;
        }

        void BuildResult()
        {
            SkillSet actual = _team.ActualSum;
            SkillSet required = Theme.RequiredSkills;
            Result = new InterviewResult(
                actual,
                FulfillmentRate.Percent(actual, required),
                _verificationSuccessCount,
                _verificationFailureCount,
                _team.FraudCount,
                ResultGradeRule.Evaluate(actual, required, Theme.CoreRole, _team.FraudCount));
        }

        static Applicant[] CopyApplicants(IReadOnlyList<Applicant> applicants)
        {
            if (applicants == null || applicants.Count == 0)
                return Array.Empty<Applicant>();

            var copy = new Applicant[applicants.Count];
            for (int i = 0; i < applicants.Count; i++)
                copy[i] = applicants[i];
            return copy;
        }

        static bool OwnsLine(Applicant applicant, string lineId)
        {
            var documents = applicant.Documents;
            for (int i = 0; i < documents.Count; i++)
            {
                var lines = documents[i].Lines;
                for (int j = 0; j < lines.Count; j++)
                {
                    if (lines[j].Id == lineId)
                        return true;
                }
            }

            return false;
        }
    }
}
