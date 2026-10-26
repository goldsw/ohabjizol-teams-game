using System.Collections;
using Ohabjizol.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    public static class CreditsSceneFlow
    {
        public const string CreditsScene = "CreditsScene";
        public const string MainMenuScene = "MainMenuScene";

        public static void Open() => SceneManager.LoadScene(CreditsScene, LoadSceneMode.Single);

        public static void ReturnMain()
        {
            MainMenuTitleOverlay.SuppressForSession();
            SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
        }
    }

    /// <summary>Fades the authored credits in, then enables the return button.</summary>
    public sealed class CreditsScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup foreground;
        [SerializeField] Button backButton;
        [SerializeField, Min(0f)] float fadeInDuration = 3f;
        Coroutine fade;
        bool leaving;

        public bool HasAuthoredUi => foreground && backButton;
        public float FadeInDuration => fadeInDuration;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (!HasAuthoredUi)
            {
                Debug.LogError("Credits require an authored foreground and return button.", this);
                return;
            }
            leaving = false;
            foreground.alpha = 0f;
            foreground.interactable = false;
            foreground.blocksRaycasts = true;
            backButton.onClick.AddListener(BackToMainMenu);
            fade = StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            // Start timing after the first drawn frame so scene loading cannot skip the fade.
            yield return new WaitForEndOfFrame();
            double started = Time.realtimeSinceStartupAsDouble;
            while (foreground && !leaving)
            {
                float progress = fadeInDuration <= 0f ? 1f :
                    Mathf.Clamp01((float)((Time.realtimeSinceStartupAsDouble - started) / fadeInDuration));
                foreground.alpha = progress;
                if (progress >= 1f) break;
                yield return null;
            }
            if (foreground && !leaving) foreground.interactable = true;
            fade = null;
        }

        public void BackToMainMenu()
        {
            if (leaving || !HasAuthoredUi || !foreground.interactable) return;
            leaving = true;
            foreground.interactable = false;
            CreditsSceneFlow.ReturnMain();
        }

        void OnDisable()
        {
            if (backButton) backButton.onClick.RemoveListener(BackToMainMenu);
            if (fade != null) StopCoroutine(fade);
            fade = null;
        }

#if UNITY_EDITOR
        public void ConfigureEditor(CanvasGroup group, Button back, float duration)
        {
            foreground = group;
            backButton = back;
            fadeInDuration = Mathf.Max(0f, duration);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
