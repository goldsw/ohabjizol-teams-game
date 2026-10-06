using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ohabjizol.Managers
{
    public enum SoundStopReason
    {
        Stopped,
        Finished
    }

    /// <summary>
    /// Spawns sound instances, plays BGM, and stores the master volume gauge.
    /// Heard volume is <see cref="SoundObject.Volume"/> times <see cref="VolumeGauge"/>.
    /// Visual spawn is delegated through <see cref="SpawnVfx"/> and <see cref="StopVfx"/>.
    /// </summary>
    public sealed class SoundManager
    {
        private readonly List<SoundObject> _spawned = new List<SoundObject>();
        private readonly ReadOnlyCollection<SoundObject> _spawnedView;
        private float _volumeGauge = 1f;

        public SoundManager()
        {
            _spawnedView = _spawned.AsReadOnly();
        }

        public IReadOnlyList<SoundObject> SpawnedSounds
        {
            get { return _spawnedView; }
        }

        /// <summary>
        /// Parameter snapshot of the BGM this manager is playing. Not a spawned instance.
        /// </summary>
        public SoundObject CurrentBgm { get; private set; }

        public bool IsBgmPlaying { get; private set; }

        /// <summary>
        /// Master multiplier, about 0–1. This manager is the only store for that gauge.
        /// </summary>
        public float VolumeGauge
        {
            get { return _volumeGauge; }
        }

        /// <summary>
        /// Caller assigns <see cref="VfxManager.SpawnFor"/>. Invoked for a spawned sound that has a <see cref="SoundObject.VfxId"/>.
        /// </summary>
        public Action<SoundObject> SpawnVfx { get; set; }

        /// <summary>
        /// Caller assigns <see cref="VfxManager.StopFor"/>. Invoked when that spawned sound stops.
        /// </summary>
        public Action<SoundObject> StopVfx { get; set; }

        public event Action<float> VolumeGaugeChanged;

        public event Action<SoundObject> SoundSpawned;

        public event Action<SoundObject, SoundStopReason> SoundStopped;

        public event Action<SoundObject> BgmStarted;

        public event Action BgmStopped;

        /// <summary>
        /// Stores the gauge. Does not rewrite <see cref="SoundObject.Volume"/>.
        /// Values outside 0–1 clamp. Non-finite values throw.
        /// </summary>
        public void SetVolumeGauge(float gauge)
        {
            _volumeGauge = ClampGauge(gauge);

            var handler = VolumeGaugeChanged;
            if (handler != null)
                handler(_volumeGauge);
        }

        /// <summary>
        /// Heard level for one sound: its own volume times <see cref="VolumeGauge"/>.
        /// </summary>
        public float HeardVolume(SoundObject sound)
        {
            if (sound == null)
                throw new ArgumentNullException(nameof(sound));

            return sound.Volume * _volumeGauge;
        }

        /// <summary>
        /// Spawns a playing sound instance. The argument stays a reusable template.
        /// A <see cref="SoundObject.VfxId"/> raises <see cref="SpawnVfx"/> and does not create a visual here.
        /// </summary>
        public SoundObject SpawnSound(SoundObject parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var spawned = parameters.CreateSpawnedCopy();
            spawned.Bind(FinishSpawn);
            spawned.MarkPlaying();
            _spawned.Add(spawned);

            if (spawned.HasVfx)
            {
                var spawnVfx = SpawnVfx;
                if (spawnVfx != null)
                    spawnVfx(spawned);
            }

            var handler = SoundSpawned;
            if (handler != null)
                handler(spawned);

            return spawned;
        }

        public void StopSound(SoundObject spawned)
        {
            if (spawned == null)
                throw new ArgumentNullException(nameof(spawned));
            if (!spawned.IsSpawned)
            {
                throw new InvalidOperationException(
                    "Only a spawned sound object can be stopped as SFX/VFX.");
            }

            FinishSpawn(spawned, SoundStopReason.Stopped);
        }

        /// <summary>
        /// A later audio backend calls this when a one-shot clip ends.
        /// These classes have no playback clock of their own.
        /// </summary>
        public void NotifySpawnedFinished(SoundObject spawned)
        {
            if (spawned == null)
                throw new ArgumentNullException(nameof(spawned));
            if (!spawned.IsSpawned)
            {
                throw new InvalidOperationException(
                    "Only a spawned sound object can finish.");
            }

            FinishSpawn(spawned, SoundStopReason.Finished);
        }

        public void StopAllSpawned()
        {
            var snapshot = _spawned.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
                FinishSpawn(snapshot[i], SoundStopReason.Stopped);
        }

        /// <summary>
        /// Plays BGM on this manager. Copies <paramref name="parameters"/> and does not spawn.
        /// Replaces the BGM already playing.
        /// </summary>
        public void PlayBgm(SoundObject parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.HasClip)
            {
                throw new ArgumentException(
                    "BGM parameters need a clip id.",
                    nameof(parameters));
            }

            if (IsBgmPlaying)
                StopBgm();

            CurrentBgm = parameters.CopyParameters();
            IsBgmPlaying = true;

            var handler = BgmStarted;
            if (handler != null)
                handler(CurrentBgm);
        }

        public void StopBgm()
        {
            if (!IsBgmPlaying)
                return;

            IsBgmPlaying = false;
            CurrentBgm = null;

            var handler = BgmStopped;
            if (handler != null)
                handler();
        }

        private void FinishSpawn(SoundObject spawned, SoundStopReason reason)
        {
            _spawned.Remove(spawned);
            if (!spawned.IsPlaying)
                return;

            spawned.MarkStopped();

            if (spawned.HasVfx)
            {
                var stopVfx = StopVfx;
                if (stopVfx != null)
                    stopVfx(spawned);
            }

            var handler = SoundStopped;
            if (handler != null)
                handler(spawned, reason);
        }

        private static float ClampGauge(float gauge)
        {
            if (float.IsNaN(gauge) || float.IsInfinity(gauge))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gauge),
                    "Volume gauge must be finite.");
            }

            if (gauge < 0f)
                return 0f;
            if (gauge > 1f)
                return 1f;
            return gauge;
        }
    }
}
