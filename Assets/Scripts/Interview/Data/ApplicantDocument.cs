using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class ApplicantDocument
    {
        public ApplicantDocument(DocumentKind kind, IReadOnlyList<DocumentLine> lines)
        {
            if (lines == null || lines.Count != 2)
                throw new ArgumentException("A document has two lines.", nameof(lines));

            Kind = kind;
            Lines = new DocumentLine[] { lines[0], lines[1] };
        }

        public DocumentKind Kind { get; }

        public IReadOnlyList<DocumentLine> Lines { get; }
    }
}
