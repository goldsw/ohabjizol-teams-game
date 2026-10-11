using System;

namespace Ohabjizol.Interview
{
    public sealed class DocumentLine
    {
        public DocumentLine(string id, string sentence, bool isCorrectSuspicion)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A document line needs an id.", nameof(id));

            Id = id;
            Sentence = sentence ?? string.Empty;
            IsCorrectSuspicion = isCorrectSuspicion;
        }

        public string Id { get; }

        public string Sentence { get; }

        public bool IsCorrectSuspicion { get; }
    }
}
