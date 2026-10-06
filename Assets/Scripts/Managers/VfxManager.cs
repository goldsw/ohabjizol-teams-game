using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// Spawns and stops visuals from a <see cref="SoundObject.VfxId"/>.
    /// Does not store the volume gauge and does not play audio.
    /// </summary>
    public sealed class VfxManager
    {
        private readonly List<VfxInstance> _active = new List<VfxInstance>();
        private readonly ReadOnlyCollection<VfxInstance> _activeView;
        private readonly Dictionary<SoundObject, VfxInstance> _bySound = new Dictionary<SoundObject, VfxInstance>();

        public VfxManager()
        {
            _activeView = _active.AsReadOnly();
        }

        public IReadOnlyList<VfxInstance> Active
        {
            get { return _activeView; }
        }

        public event Action<VfxInstance> VfxSpawned;

        public event Action<VfxInstance> VfxStopped;

        public VfxInstance Spawn(string vfxId)
        {
            var instance = Create(vfxId);
            PublishSpawn(instance);
            return instance;
        }

        /// <summary>
        /// Action target for <see cref="SoundManager.SpawnVfx"/>.
        /// Reads <see cref="SoundObject.VfxId"/> and spawns a visual for that sound.
        /// </summary>
        public void SpawnFor(SoundObject sound)
        {
            if (sound == null)
                throw new ArgumentNullException(nameof(sound));
            if (!sound.HasVfx)
                return;
            if (_bySound.ContainsKey(sound))
                return;

            var instance = Create(sound.VfxId);
            _bySound[sound] = instance;
            PublishSpawn(instance);
        }

        public void Stop(VfxInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Unlink(instance);
            _active.Remove(instance);
            if (!instance.IsPlaying)
                return;

            instance.MarkStopped();

            var handler = VfxStopped;
            if (handler != null)
                handler(instance);
        }

        /// <summary>
        /// Action target for <see cref="SoundManager.StopVfx"/>.
        /// </summary>
        public void StopFor(SoundObject sound)
        {
            if (sound == null)
                throw new ArgumentNullException(nameof(sound));

            VfxInstance instance;
            if (!_bySound.TryGetValue(sound, out instance))
                return;

            Stop(instance);
        }

        private VfxInstance Create(string vfxId)
        {
            if (string.IsNullOrWhiteSpace(vfxId))
                throw new ArgumentException("Vfx id is required.", nameof(vfxId));

            var instance = new VfxInstance(vfxId.Trim());
            instance.Bind(Stop);
            instance.MarkPlaying();
            _active.Add(instance);
            return instance;
        }

        private void PublishSpawn(VfxInstance instance)
        {
            var handler = VfxSpawned;
            if (handler != null)
                handler(instance);
        }

        private void Unlink(VfxInstance instance)
        {
            SoundObject owner = null;
            foreach (var pair in _bySound)
            {
                if (pair.Value == instance)
                {
                    owner = pair.Key;
                    break;
                }
            }

            if (owner != null)
                _bySound.Remove(owner);
        }
    }
}
