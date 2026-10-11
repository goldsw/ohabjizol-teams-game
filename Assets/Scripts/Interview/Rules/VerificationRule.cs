namespace Ohabjizol.Interview
{
    public static class VerificationRule
    {
        // PROTOTYPE ONLY
        // 최종 기획 확정 후 교체 예정

        // PROTOTYPE RULE
        // Final verification scoring is not yet defined.
        public static VerificationOutcome Evaluate(Applicant applicant, string selectedLineId)
        {
            if (applicant == null)
                return VerificationOutcome.Failure;

            if (IsCorrectLine(applicant, selectedLineId))
                return VerificationOutcome.Success;

            if (!string.IsNullOrWhiteSpace(selectedLineId))
                return VerificationOutcome.Failure;

            if (applicant.Kind == ApplicantKind.Fraud)
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
