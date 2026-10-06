using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// One spawned visual. Only <see cref="VfxManager"/> creates it.
    /// </summary>
    public sealed class VfxInstance
    {
        private Action<VfxInstance> _requestStop;

        internal VfxInstance(string vfxId)
        {
            VfxId = vfxId;
        }

        public string VfxId { get; }

        public bool IsPlaying { get; private set; }

        public void Stop()
        {
            if (!IsPlaying)
                return;
            if (_requestStop == null)
                throw new InvalidOperationException("This visual is not bound to a VfxManager.");

            _requestStop(this);
        }

        internal void Bind(Action<VfxInstance> requestStop)
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
    }
}
