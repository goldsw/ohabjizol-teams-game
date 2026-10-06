using System;

namespace Ohabjizol.Managers
{
    public enum GamePhase
    {
        Idle,
        Running,
        Ended
    }

    /// <summary>
    /// Invocation points for a session. Start and end only; the screening loop
    /// subscribes to the events and owns the rules.
    /// </summary>
    public sealed class GameManager
    {
        public GamePhase Phase { get; private set; }

        public event Action GameStarted;

        public event Action GameEnded;

        /// <summary>
        /// Starts a session from <see cref="GamePhase.Idle"/> or after
        /// <see cref="EndGame"/> has returned the phase to <see cref="GamePhase.Ended"/>.
        /// </summary>
        public void StartGame()
        {
            if (Phase == GamePhase.Running)
                throw new InvalidOperationException("Game is already running.");

            Phase = GamePhase.Running;

            var handler = GameStarted;
            if (handler != null)
                handler();
        }

        public void EndGame()
        {
            if (Phase != GamePhase.Running)
                throw new InvalidOperationException("Game is not running.");

            Phase = GamePhase.Ended;

            var handler = GameEnded;
            if (handler != null)
                handler();
        }
    }
}
