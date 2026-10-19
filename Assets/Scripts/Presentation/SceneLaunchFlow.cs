using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ohabjizol.Presentation
{
    public readonly struct SceneLaunchSnapshot
    {
        public float Progress { get; }
        public bool IsComplete { get; }
        public bool CanBeginZoom { get; }

        public SceneLaunchSnapshot(float progress, bool isComplete, bool canBeginZoom)
        {
            Progress = progress;
            IsComplete = isComplete;
            CanBeginZoom = canBeginZoom;
        }
    }

    /// <summary>One launch per instance. Pending Unity loads cannot be cancelled.</summary>
    public sealed class SceneLaunchFlow
    {
        readonly float _minimumDuration;
        readonly float _progressSpeed;
        readonly float _completeHold;
        AsyncOperation _operation;
        float _elapsed;
        float _progress;
        float _completeElapsed;
        bool _complete;

        public SceneLaunchFlow(float minimumLoadingDuration, float loadingProgressSpeed, float completeHoldDuration)
        {
            _minimumDuration = Mathf.Max(0f, minimumLoadingDuration);
            _progressSpeed = loadingProgressSpeed;
            _completeHold = Mathf.Max(0f, completeHoldDuration);
        }

        public bool IsStarted => _operation != null;
        public bool IsActivated { get; private set; }
        public bool CanBeginZoom => IsStarted && _complete && _completeElapsed >= _completeHold;
        public SceneLaunchSnapshot Snapshot => new SceneLaunchSnapshot(_progress, _complete, CanBeginZoom);

        /// <summary>Rejects the active scene (name or asset path), including MainMenu/Loading self-loads.</summary>
        public bool TryStart(string sceneName, out string error)
        {
            error = null;
            if (IsStarted)
            {
                error = "Scene launch has already started; pending loads cannot be cancelled.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                error = "Scene is missing or not enabled in build settings: " + sceneName;
                return false;
            }
            var active = SceneManager.GetActiveScene();
            string normalized = sceneName.Replace((char)92, '/');
            string shortName = System.IO.Path.GetFileNameWithoutExtension(normalized);
            if (normalized == active.path || shortName == active.name)
            {
                error = "Scene launch cannot reload the active scene: " + sceneName;
                return false;
            }
            try
            {
                _operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (_operation != null)
                {
                    _operation.allowSceneActivation = false;
                    return true;
                }
                error = "Could not start async load for " + sceneName;
            }
            catch (Exception exception)
            {
                error = "Could not start async load for " + sceneName + ": " + exception.Message;
            }
            return false;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsStarted || IsActivated || float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime))
                return;
            float delta = Mathf.Max(0f, unscaledDeltaTime);
            if (_complete)
            {
                _completeElapsed += delta;
                return;
            }
            _elapsed += delta;
            bool loadReady = _operation.progress >= 0.9f;
            bool timeReady = _elapsed >= _minimumDuration;
            float target = loadReady ? 0.92f : Mathf.Clamp01(_operation.progress / 0.9f) * 0.92f;
            if (loadReady && timeReady)
                target = 1f;
            _progress = _progressSpeed <= 0f ? target : Mathf.MoveTowards(_progress, target, _progressSpeed * delta);
            if (loadReady && timeReady && _progress >= 0.999f)
            {
                _progress = 1f;
                _complete = true;
            }
        }

        /// <summary>Call only from the zoom completion callback; activation is accepted once.</summary>
        public bool TryActivate()
        {
            if (!CanBeginZoom || IsActivated)
                return false;
            IsActivated = true;
            _operation.allowSceneActivation = true;
            return true;
        }
    }
}
