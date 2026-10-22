using System;
using Ohabjizol.Presentation;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Ohabjizol.Interview
{
    public sealed class InGameSettingsLauncher : MonoBehaviour
    {
        [SerializeField] Button launcher;
        [SerializeField] SettingsWindow window;
        [SerializeField] Button restart;
        [SerializeField] Button returnToTitle;
        SettingsApplier applier;
        bool initialized;
        bool restarting;

        public bool HasAuthoredUi => launcher != null && window != null && window.HasAuthoredContent;

        void Awake()
        {
            if (!HasAuthoredUi)
            {
                Debug.LogError("In-game settings require an authored button and SettingsWindow.", this);
                enabled = false;
                return;
            }
            window.InitializeAuthored();
            if (!window.Root || !window.Info || !window.Toolbar)
            {
                Debug.LogError("In-game SettingsWindow frame references are incomplete.", this);
                enabled = false;
                return;
            }
            applier = SettingsApplier.LoadAndApply();
            window.Bind(applier, null);
            window.Close();
            initialized = true;
        }

        void OnEnable()
        {
            if (!initialized) return;
            launcher.onClick.RemoveListener(Toggle);
            launcher.onClick.AddListener(Toggle);
            if (restart)
            {
                restart.onClick.RemoveListener(RestartGame);
                restart.onClick.AddListener(RestartGame);
            }
            if (returnToTitle)
            {
                returnToTitle.onClick.RemoveListener(ReturnToTitle);
                returnToTitle.onClick.AddListener(ReturnToTitle);
            }
        }

        void OnDisable()
        {
            if (launcher) launcher.onClick.RemoveListener(Toggle);
            if (restart) restart.onClick.RemoveListener(RestartGame);
            if (returnToTitle) returnToTitle.onClick.RemoveListener(ReturnToTitle);
            if (initialized && window) window.Close();
            applier?.Flush();
        }

        void OnDestroy()
        {
            if (window) window.Unbind();
            applier?.Flush();
        }

        void OnApplicationPause(bool paused) { if (paused) applier?.Flush(); }
        void OnApplicationQuit() => applier?.Flush();

        void Toggle()
        {
            if (!initialized || !isActiveAndEnabled) return;
            if (window.IsOpen) window.Minimize();
            else Open();
        }

        public void Open()
        {
            if (!initialized || !isActiveAndEnabled) return;
            window.transform.SetAsLastSibling();
            if (window.IsMinimized) window.Restore();
            else window.Open();
        }

        void RestartGame()
        {
            if (!initialized || !isActiveAndEnabled || restarting) return;
            restarting = true;
            if (restart) restart.interactable = false;
            try
            {
                var screen = gameObject.GetComponentInParent<InterviewScreen>();
                if (!screen) throw new InvalidOperationException("Restart requires a parent InterviewScreen.");
                screen.DisposeSession();
                window.Close();
                applier?.Flush();
                var run = CampaignRunStore.StartNewRun();
                StageSelection.Set(run.NextStage);
                ResultCarry.Clear();
                SceneManager.LoadScene("InterviewScene", LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                // Keep the guard latched: a partially disposed/reset session cannot be retried safely.
                Debug.LogError("In-game restart failed; further restart attempts are disabled. " + exception, this);
            }
        }

        void ReturnToTitle()
        {
            if (!initialized || !isActiveAndEnabled || restarting) return;
            restarting = true;
            if (returnToTitle) returnToTitle.interactable = false;
            if (restart) restart.interactable = false;
            try
            {
                var screen = gameObject.GetComponentInParent<InterviewScreen>();
                if (screen) screen.DisposeSession();
                window.Close();
                applier?.Flush();
                ResultCarry.Clear();
                SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogError("Return to title failed; further attempts are disabled. " + exception, this);
            }
        }

#if UNITY_EDITOR
        public void ConfigureReturnToTitleEditor(Button button)
        {
            if (!button || !button.transform.IsChildOf(transform))
                throw new InvalidOperationException("Return-to-title button must belong to this launcher.");
            returnToTitle = button;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ConfigureRestartEditor(Button button)
        {
            if (!button || !button.transform.IsChildOf(transform))
                throw new InvalidOperationException("Restart button must belong to this launcher.");
            restart = button;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ConfigureEditor(Button button, SettingsWindow settingsWindow)
        {
            launcher = button;
            window = settingsWindow;
            if (!HasAuthoredUi) throw new InvalidOperationException("Authored button and settings content required.");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
