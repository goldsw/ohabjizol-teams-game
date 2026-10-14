using System;
using UnityEngine;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Normalized anchors for the interview overlay, measured on a 1920 × 1080 call backdrop.
    /// x and y are the minimum corner. Width and height are the span.
    /// </summary>
    [Serializable]
    public sealed class InterviewLayout
    {
        public Rect Background = new Rect(0f, 0f, 1f, 1f);
        public Rect Title = new Rect(0.33f, 0.90f, 0.47f, 0.07f);
        public Rect MainVideo = new Rect(0.332f, 0.344f, 0.611f, 0.495f);
        public Rect Tile1 = new Rect(0.438f, 0.165f, 0.129f, 0.153f);
        public Rect Tile2 = new Rect(0.578f, 0.165f, 0.128f, 0.153f);
        public Rect Tile3 = new Rect(0.718f, 0.165f, 0.128f, 0.153f);
        public Rect LeftPanel = new Rect(0.073f, 0.315f, 0.181f, 0.555f);
        public Rect Suspicion = new Rect(0.29f, 0.07f, 0.33f, 0.06f);
        public Rect Accept = new Rect(0.66f, 0.065f, 0.15f, 0.075f);
        public Rect Reject = new Rect(0.825f, 0.065f, 0.16f, 0.075f);
        public Rect Taskbar = new Rect(0f, 0f, 1f, 0.052f);
    }
}
