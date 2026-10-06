using System;
using System.Collections.Generic;

namespace Ohabjizol.Managers
{
    public enum StagePhase
    {
        Idle,
        Running,
        Ended
    }

    /// <summary>
    /// Sounds and events for the current stage, plus stage start, stage end,
    /// and score settlement. Does not run the applicant-screening rules.
    /// </summary>
    public sealed class StageManager : IDisposable
    {
        private readonly SoundManager _sound;
        private readonly GameManager _game;
        private readonly List<SoundObject> _stageSounds = new List<SoundObject>();
        private bool _stageOwnsBgm;
        private bool _ending;
        private bool _disposed;

        public StageManager(SoundManager soundManager, GameManager gameManager)
        {
            if (soundManager == null)
                throw new ArgumentNullException(nameof(soundManager));
            if (gameManager == null)
                throw new ArgumentNullException(nameof(gameManager));

            _sound = soundManager;
            _game = gameManager;
            _game.GameStarted += HandleGameStarted;
            _game.GameEnded += HandleGameEnded;
            _sound.SoundStopped += HandleSoundStopped;
        }

        public StagePhase Phase { get; private set; }

        public StageContext Current { get; private set; }

        public ScoreSettlement LastSettlement { get; private set; }

        public event Action<StageContext> StageStarted;

        public event Action<StageContext> StageEnded;

        public event Action<ScoreSettlement> ScoreSettled;

        public event Action<StageEvent> StageEventRaised;

        public void StartStage(StageContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (_game.Phase != GamePhase.Running)
                throw new InvalidOperationException("Start the game before starting a stage.");
            if (Phase == StagePhase.Running)
                throw new InvalidOperationException("A stage is already running.");
            if (context.Bgm != null && !context.Bgm.HasClip)
            {
                throw new ArgumentException(
                    "Stage BGM needs a clip id.",
                    nameof(context));
            }

            Current = context;
            LastSettlement = null;
            _stageSounds.Clear();
            _stageOwnsBgm = false;
            Phase = StagePhase.Running;

            if (context.Bgm != null)
            {
                _sound.PlayBgm(context.Bgm);
                _stageOwnsBgm = true;
            }

            var handler = StageStarted;
            if (handler != null)
                handler(context);
        }

        public void EndStage()
        {
            if (_ending)
                return;
            if (Phase != StagePhase.Running)
                throw new InvalidOperationException("No stage is running.");

            _ending = true;
            try
            {
                StopStageSounds();
                if (_stageOwnsBgm)
                {
                    _sound.StopBgm();
                    _stageOwnsBgm = false;
                }

                Phase = StagePhase.Ended;

                var handler = StageEnded;
                if (handler != null)
                    handler(Current);
            }
            finally
            {
                _ending = false;
            }
        }

        /// <summary>
        /// Records a settlement the caller already computed. Legal while the stage
        /// is running and after it has ended, until the next stage or game starts.
        /// </summary>
        public void SettleScore(ScoreSettlement settlement)
        {
            if (settlement == null)
                throw new ArgumentNullException(nameof(settlement));
            if (Current == null || Phase == StagePhase.Idle)
                throw new InvalidOperationException("No stage to settle.");
            if (!string.Equals(settlement.StageId, Current.StageId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Settlement stage id does not match the current stage.",
                    nameof(settlement));
            }

            if (LastSettlement != null)
                throw new InvalidOperationException("This stage is already settled.");

            LastSettlement = settlement;

            var handler = ScoreSettled;
            if (handler != null)
                handler(settlement);
        }

        /// <summary>
        /// Spawns an SFX or VFX for this stage. EndStage stops it if it is still playing.
        /// </summary>
        public SoundObject PlayStageSound(SoundObject parameters)
        {
            EnsureRunning();
            var spawned = _sound.SpawnSound(parameters);
            if (spawned.IsPlaying)
                _stageSounds.Add(spawned);
            return spawned;
        }

        public void PlayStageBgm(SoundObject parameters)
        {
            EnsureRunning();
            _sound.PlayBgm(parameters);
            _stageOwnsBgm = true;
        }

        public void RaiseStageEvent(StageEvent stageEvent)
        {
            if (stageEvent == null)
                throw new ArgumentNullException(nameof(stageEvent));

            EnsureRunning();

            var handler = StageEventRaised;
            if (handler != null)
                handler(stageEvent);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _game.GameStarted -= HandleGameStarted;
            _game.GameEnded -= HandleGameEnded;
            _sound.SoundStopped -= HandleSoundStopped;
        }

        private void EnsureRunning()
        {
            if (Phase != StagePhase.Running)
                throw new InvalidOperationException("No stage is running.");
        }

        private void StopStageSounds()
        {
            var snapshot = _stageSounds.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].IsPlaying)
                    _sound.StopSound(snapshot[i]);
            }

            _stageSounds.Clear();
        }

        private void HandleSoundStopped(SoundObject sound, SoundStopReason reason)
        {
            _stageSounds.Remove(sound);
        }

        private void HandleGameStarted()
        {
            if (Phase == StagePhase.Running)
                EndStage();

            Current = null;
            LastSettlement = null;
            _stageSounds.Clear();
            _stageOwnsBgm = false;
            Phase = StagePhase.Idle;
        }

        private void HandleGameEnded()
        {
            if (Phase == StagePhase.Running)
                EndStage();
        }
    }
}
