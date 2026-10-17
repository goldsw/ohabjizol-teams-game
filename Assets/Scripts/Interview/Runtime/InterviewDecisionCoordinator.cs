using System;
using System.Collections;
using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class InterviewDecisionCoordinator : IDisposable
    {
        readonly MonoBehaviour _host;
        readonly StageManager _stage;
        readonly Func<bool, float> _presentDecision;
        Coroutine _wait;
        Applicant _captured;
        int _index;
        int _generation;
        float _remaining;
        bool _accepted;
        bool _active;
        bool _disposed;
        bool _committing;

        public InterviewDecisionCoordinator(MonoBehaviour host, StageManager stage,
            Func<bool, float> presentDecision)
        {
            _host = host ? host : throw new ArgumentNullException(nameof(host));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));
            _presentDecision = presentDecision ?? throw new ArgumentNullException(nameof(presentDecision));
            _active = host.isActiveAndEnabled;
        }

        public bool IsPending { get; private set; }
        public event Action Changed;

        public void Request(bool accepted)
        {
            if (_disposed || !_active || !_host || !_host.isActiveAndEnabled
                || IsPending || _stage.IsDeciding) return;
            var session = _stage.Session;
            if (session.IsFinished) { _stage.RetryCompletion(); return; }
            if (session.Current == null) return;
            _captured = session.Current;
            _index = session.CurrentIndex;
            _accepted = accepted;
            _remaining = 0f;
            ++_generation;
            IsPending = true;
            try
            {
                Changed?.Invoke();
                if (_disposed || !IsPending) return;
                float duration = _presentDecision(accepted);
                if (_disposed || !IsPending) return;
                _remaining = float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f
                    ? 3f : duration;
                if (_active && _host && _host.isActiveAndEnabled) StartWait();
            }
            catch (Exception error)
            {
                Cancel();
                Debug.LogException(error, _host);
            }
        }

        public void Suspend()
        {
            if (_disposed) return;
            _active = false;
            ++_generation;
            StopWait();
        }

        public void Resume()
        {
            if (_disposed || !_host || !_host.isActiveAndEnabled) return;
            _active = true;
            if (IsPending && !_committing && _wait == null) StartWait();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _active = false;
            ++_generation;
            StopWait();
            Release();
        }

        void StartWait()
        {
            int generation = _generation;
            var wait = _host.StartCoroutine(Wait(generation));
            if (IsPending && generation == _generation) _wait = wait;
        }

        IEnumerator Wait(int generation)
        {
            while (_remaining > 0f)
            {
                yield return null;
                if (!CanContinue(generation)) yield break;
                _remaining = Mathf.Max(0f, _remaining - Time.unscaledDeltaTime);
            }
            if (!CanContinue(generation)) yield break;
            _wait = null;
            if (_stage.IsDeciding || _stage.Session.IsFinished
                || !ReferenceEquals(_captured, _stage.Session.Current)
                || _index != _stage.Session.CurrentIndex)
            {
                Cancel();
                yield break;
            }
            _committing = true;
            try { _stage.Decide(_accepted); }
            finally
            {
                _committing = false;
                Release();
            }
        }

        bool CanContinue(int generation) => !_disposed && _active && IsPending
            && generation == _generation && _host && _host.isActiveAndEnabled;

        void StopWait()
        {
            if (_wait != null && _host) _host.StopCoroutine(_wait);
            _wait = null;
        }

        void Cancel()
        {
            ++_generation;
            StopWait();
            Release();
        }

        void Release()
        {
            if (!IsPending) return;
            IsPending = false;
            _captured = null;
            _remaining = 0f;
            Changed?.Invoke();
        }
    }
}
