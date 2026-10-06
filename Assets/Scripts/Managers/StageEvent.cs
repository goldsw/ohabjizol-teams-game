using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// A named beat the current stage raises. The later game loop chooses the name.
    /// </summary>
    public sealed class StageEvent
    {
        public StageEvent(string name, string payload = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Event name is required.", nameof(name));

            Name = name.Trim();
            Payload = payload ?? "";
        }

        public string Name { get; }

        public string Payload { get; }
    }
}
