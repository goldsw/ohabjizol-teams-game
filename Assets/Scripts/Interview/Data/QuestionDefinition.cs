using System;
namespace Ohabjizol.Interview
{
    public sealed class QuestionDefinition
    {
        public QuestionDefinition(string id, string prompt)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A question needs an id.", nameof(id));

            Id = id;
            Prompt = prompt ?? string.Empty;
        }
        public string Id { get; }
        public string Prompt { get; }
    }
}
