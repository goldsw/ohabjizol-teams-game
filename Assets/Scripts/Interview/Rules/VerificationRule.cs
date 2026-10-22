namespace Ohabjizol.Interview
{
    public static class VerificationRule
    {
        // Document evidence is evaluated against the portfolio state saved for this session.
        public static VerificationOutcome Evaluate(Applicant applicant, string selectedLineId)
        {
            if (applicant == null)
                return VerificationOutcome.Failure;

            if (applicant.HasFalsePortfolio && IsCorrectLine(applicant, selectedLineId))
                return VerificationOutcome.Success;

            if (!string.IsNullOrWhiteSpace(selectedLineId))
                return VerificationOutcome.Failure;

            if (applicant.HasFalsePortfolio)
                return VerificationOutcome.Failure;

            return VerificationOutcome.None;
        }

        static bool IsCorrectLine(Applicant applicant, string selectedLineId)
        {
            if (string.IsNullOrWhiteSpace(selectedLineId))
                return false;

            var documents = applicant.Documents;
            for (int i = 0; i < documents.Count; i++)
            {
                var lines = documents[i].Lines;
                for (int j = 0; j < lines.Count; j++)
                {
                    DocumentLine line = lines[j];
                    if (line.Id == selectedLineId && line.IsCorrectSuspicion)
                        return true;
                }
            }

            return false;
        }
    }
}
