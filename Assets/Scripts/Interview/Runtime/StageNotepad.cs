using System;

namespace Ohabjizol.Interview
{
    public sealed class StageNotepad
    {
        public string Content { get; private set; } = string.Empty;

        public event Action<string> Changed;

        public void SetContent(string content)
        {
            content = content ?? string.Empty;
            if (Content == content) return;
            Content = content;
            Changed?.Invoke(content);
        }

        public void Clear()
        {
            SetContent(string.Empty);
        }
    }
}
