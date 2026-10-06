using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// Identity of a stage and the optional BGM template to play when it starts.
    /// </summary>
    public sealed class StageContext
    {
        public StageContext(string stageId, SoundObject bgm = null)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("Stage id is required.", nameof(stageId));

            StageId = stageId.Trim();
            Bgm = bgm;
        }

        public string StageId { get; }

        public SoundObject Bgm { get; }
    }
}
