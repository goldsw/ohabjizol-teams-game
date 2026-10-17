using System;

namespace Ohabjizol.Presentation
{
    /// <summary>Initial desktop window visibility saved on the menu host.</summary>
    [Serializable]
    public sealed class DesktopInitialState
    {
        public bool codeOpen = false;
        public bool settingsOpen = false;
        public bool tutorialOpen = false;
    }
}
