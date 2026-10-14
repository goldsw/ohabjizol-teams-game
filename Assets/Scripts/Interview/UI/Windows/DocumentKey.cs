using System;

namespace Ohabjizol.Interview
{
    public readonly struct DocumentKey : IEquatable<DocumentKey>
    {
        public string ApplicantKey { get; }
        public DocumentKind Kind { get; }
        public DocumentKey(string applicantKey, DocumentKind kind)
        {
            if (string.IsNullOrWhiteSpace(applicantKey)) throw new ArgumentException("Applicant key required.", nameof(applicantKey));
            if (kind != DocumentKind.Application && kind != DocumentKind.Portfolio)
                throw new ArgumentOutOfRangeException(nameof(kind));
            ApplicantKey = applicantKey;
            Kind = kind;
        }
        public bool Equals(DocumentKey other) => Kind == other.Kind &&
            string.Equals(ApplicantKey, other.ApplicantKey, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is DocumentKey other && Equals(other);
        public override int GetHashCode() => unchecked(((ApplicantKey == null ? 0 :
            StringComparer.Ordinal.GetHashCode(ApplicantKey)) * 397) ^ (int)Kind);
        public static bool operator ==(DocumentKey left, DocumentKey right) => left.Equals(right);
        public static bool operator !=(DocumentKey left, DocumentKey right) => !left.Equals(right);
    }
}
