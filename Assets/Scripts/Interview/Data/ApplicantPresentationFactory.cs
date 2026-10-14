using System;
using System.Text;
namespace Ohabjizol.Interview
{
    public static class ApplicantPresentationFactory
    {
        public static ApplicantDocumentPacket Create(Applicant applicant, string applicantKey, PortfolioDisplayState displayState)
        {
            if (applicant == null) throw new ArgumentNullException(nameof(applicant));
            return new ApplicantDocumentPacket(applicantKey, applicant.Name, applicant.PrimaryRole,
                Text(applicant, DocumentKind.Application), Text(applicant, DocumentKind.Portfolio), applicant.TruePortfolioQuality);
        }
        static string Text(Applicant applicant, DocumentKind kind)
        {
            var text = new StringBuilder();
            foreach (var document in applicant.Documents)
            {
                if (document == null || document.Kind != kind) continue;
                foreach (var line in document.Lines)
                {
                    if (line == null) continue;
                    if (text.Length > 0) text.Append('\n');
                    text.Append(line.Sentence);
                }
            }
            return text.ToString();
        }
    }
}
