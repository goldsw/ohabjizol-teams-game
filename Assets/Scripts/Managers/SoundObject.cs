using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// Parameter template and spawned playback instance for one sound.
    /// <see cref="SoundManager.SpawnSound"/> creates a new instance.
    /// <see cref="SoundManager.PlayBgm"/> reads an instance as parameters.
    /// <see cref="VfxId"/> is only a visual cue. <see cref="VfxManager"/> spawns it
    /// when <see cref="SoundManager.SpawnVfx"/> points at that manager.
    /// </summary>
    public sealed class SoundObject
    {
        private bool _spawned;
        private Action<SoundObject, SoundStopReason> _requestStop;

        public SoundObject(
            string clipId = null,
            float volume = 1f,
            float pitch = 1f,
            bool loop = false,
            float fadeInSeconds = 0f,
            float fadeOutSeconds = 0f,
            string vfxId = null)
        {
            ClipId = NormalizeOptional(clipId);
            VfxId = NormalizeOptional(vfxId);
            if (ClipId.Length == 0 && VfxId.Length == 0)
            {
                throw new ArgumentException(
                    "A sound object needs a clip id, a vfx id, or both.");
            }

            Volume = RequireFiniteNonNegative(volume, nameof(volume));
            Pitch = RequireFinitePositive(pitch, nameof(pitch));
            FadeInSeconds = RequireFiniteNonNegative(fadeInSeconds, nameof(fadeInSeconds));
            FadeOutSeconds = RequireFiniteNonNegative(fadeOutSeconds, nameof(fadeOutSeconds));
            Loop = loop;
        }

        public string ClipId { get; }

        public string VfxId { get; }

        public float Volume { get; }

        public float Pitch { get; }

        public bool Loop { get; }

        public float FadeInSeconds { get; }

        public float FadeOutSeconds { get; }

        public bool HasClip
        {
            get { return ClipId.Length > 0; }
        }

        public bool HasVfx
        {
            get { return VfxId.Length > 0; }
        }

        /// <summary>
        /// True only on instances created by <see cref="SoundManager.SpawnSound"/>.
        /// Parameter objects stay false so the same template can be reused.
        /// </summary>
        public bool IsSpawned
        {
            get { return _spawned; }
        }

        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Stops a spawned SFX/VFX instance. Parameter templates cannot stop themselves;
        /// BGM is stopped with <see cref="SoundManager.StopBgm"/>.
        /// </summary>
        public void Stop()
        {
            if (!_spawned)
            {
                throw new InvalidOperationException(
                    "This sound object is a parameter template. Spawn it for SFX/VFX, or pass it to PlayBgm.");
            }

            if (!IsPlaying)
                return;

            if (_requestStop == null)
            {
                throw new InvalidOperationException(
                    "This spawned sound is not bound to a SoundManager.");
            }

            _requestStop(this, SoundStopReason.Stopped);
        }

        internal SoundObject CreateSpawnedCopy()
        {
            var copy = new SoundObject(
                ClipId,
                Volume,
                Pitch,
                Loop,
                FadeInSeconds,
                FadeOutSeconds,
                VfxId);
            copy._spawned = true;
            return copy;
        }

        internal SoundObject CopyParameters()
        {
            return new SoundObject(
                ClipId,
                Volume,
                Pitch,
                Loop,
                FadeInSeconds,
                FadeOutSeconds,
                VfxId);
        }

        internal void Bind(Action<SoundObject, SoundStopReason> requestStop)
        {
            _requestStop = requestStop;
        }

        internal void MarkPlaying()
        {
            IsPlaying = true;
        }

        internal void MarkStopped()
        {
            IsPlaying = false;
        }

        private static string NormalizeOptional(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            return value.Trim();
        }

        private static float RequireFiniteNonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    "Value must be finite and >= 0.");
            }

            return value;
        }

        private static float RequireFinitePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    "Value must be finite and > 0.");
            }

            return value;
        }
    }
}
