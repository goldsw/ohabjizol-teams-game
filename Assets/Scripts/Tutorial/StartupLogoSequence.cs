using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Tutorial
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1200)]
    public sealed class StartupLogoSequence : MonoBehaviour
    {
        [SerializeField] Canvas overlayCanvas;
        [SerializeField] Image shield;
        [SerializeField] CanvasGroup[] pages = new CanvasGroup[2];
        [SerializeField, Min(.1f)] float partDuration = 1.5f;
        [SerializeField, Min(.01f)] float fadeDuration = .3f;

        static bool seen;
        static StartupLogoSequence active;
        static int blockedThroughFrame = -1;
        bool showing;
        bool firstRenderComplete;
        bool discardFirstDelta;
        bool advanceNextFrame;
        int pageIndex;
        float pageElapsed;

        public static bool IsBlocking => (active && active.showing)
            || Time.frameCount <= blockedThroughFrame;

        public bool HasAuthoredUi => overlayCanvas && shield && pages != null && pages.Length == 2
            && overlayCanvas.gameObject == gameObject
            && overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            && overlayCanvas.sortingOrder > 2000
            && GetComponent<CanvasScaler>() && GetComponent<GraphicRaycaster>()
            && shield.transform.IsChildOf(transform) && shield.raycastTarget
            && pages[0] && pages[1] && pages[0] != pages[1]
            && pages[0].transform.IsChildOf(shield.transform)
            && pages[1].transform.IsChildOf(shield.transform);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            seen = false;
            active = null;
            blockedThroughFrame = -1;
        }

        void Awake()
        {
            if (seen)
            {
                SetVisible(false);
                return;
            }
            if (!HasAuthoredUi)
            {
                Debug.LogError("Startup logos require two authored pages, a shield and an overlay canvas.", this);
                SetVisible(false);
                return;
            }
            seen = true;
            active = this;
            showing = true;
            firstRenderComplete = false;
            discardFirstDelta = true;
            advanceNextFrame = false;
            pageIndex = 0;
            pageElapsed = 0f;
            SetVisible(true);
            ShowPart(0, 0f);
        }

        System.Collections.IEnumerator Start()
        {
            if (!showing) yield break;
            // Scene loading and the first render must finish before any logo time is counted.
            yield return new WaitForEndOfFrame();
            firstRenderComplete = true;
        }

        void Update()
        {
            if (!showing || !firstRenderComplete) return;
            if (discardFirstDelta)
            {
                discardFirstDelta = false;
                return;
            }
            if (advanceNextFrame)
            {
                pageIndex++;
                if (pageIndex >= pages.Length)
                {
                    showing = false;
                    blockedThroughFrame = Time.frameCount + 1;
                    SetVisible(false);
                    if (active == this) active = null;
                }
                else
                {
                    pageElapsed = 0f;
                    advanceNextFrame = false;
                    ShowPart(pageIndex, 0f);
                }
                return;
            }
            float duration = Mathf.Max(.1f, partDuration);
            // A later stall must not consume an entire fade in a single rendered frame.
            float step = Mathf.Min(Time.unscaledDeltaTime, .1f);
            pageElapsed = Mathf.Min(pageElapsed + step, duration);
            float fade = Mathf.Clamp(fadeDuration, .01f, duration * .5f);
            float alpha = Mathf.Min(pageElapsed / fade, (duration - pageElapsed) / fade);
            ShowPart(pageIndex, Mathf.Clamp01(alpha));
            advanceNextFrame = pageElapsed >= duration;
        }

        void ShowPart(int index, float alpha)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].gameObject.SetActive(i == index);
                pages[i].alpha = i == index ? alpha : 0f;
                pages[i].interactable = false;
                pages[i].blocksRaycasts = false;
            }
        }

        void SetVisible(bool visible)
        {
            if (overlayCanvas) overlayCanvas.enabled = visible;
            if (shield) shield.gameObject.SetActive(visible);
            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster) raycaster.enabled = visible;
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;
            showing = false;
            if (active == this) active = null;
            SetVisible(false);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(Canvas canvas, Image inputShield, CanvasGroup[] authoredPages)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Author startup logos in Edit Mode.");
            overlayCanvas = canvas;
            shield = inputShield;
            pages = authoredPages == null ? null : (CanvasGroup[])authoredPages.Clone();
            if (!HasAuthoredUi) throw new InvalidOperationException("Two authored startup pages required.");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
