namespace Ohabjizol.Interview
{
    public sealed class DocumentDownloadRequest
    {
        public string ApplicantKey { get; }
        public int Generation { get; }
        public DocumentKind Kind { get; }

        public DocumentDownloadRequest(string applicantKey, int generation, DocumentKind kind)
        {
            ApplicantKey = applicantKey;
            Generation = generation;
            Kind = kind;
        }
    }
}