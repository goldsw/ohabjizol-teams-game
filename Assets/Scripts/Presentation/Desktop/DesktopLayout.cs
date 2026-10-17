using System;
using UnityEngine;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Pixel placement on the 1600 × 900 monitor canvas. Icons run down from the top-left corner.
    /// Window positions are offsets from the center of the area above the taskbar.
    /// </summary>
    [Serializable]
    public sealed class DesktopLayout
    {
        public Vector2 IconOrigin = new Vector2(28f, 28f);
        public Vector2 IconCell = new Vector2(120f, 128f);
        public float IconTile = 72f;
        public float IconSpacing = 140f;
        public float TaskbarHeight = 52f;
        public Vector2 CodeWindowSize = new Vector2(1040f, 640f);
        public Vector2 CodeWindowPosition = new Vector2(60f, 0f);
        public Vector2 SettingsWindowSize = new Vector2(680f, 620f);
        public Vector2 SettingsWindowPosition = new Vector2(120f, 0f);
        public Vector2 TutorialWindowSize = new Vector2(560f, 320f);
        public Vector2 TutorialWindowPosition = new Vector2(80f, 60f);
    }
}
