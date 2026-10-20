using System;
using Ohabjizol.Interview;
using UnityEngine;

namespace Ohabjizol.Tutorial
{
    [DefaultExecutionOrder(-1000)]
    public sealed class TutorialSceneBootstrap : MonoBehaviour
    {
        [SerializeField] GameObject screenWrapper;
        [SerializeField] InterviewScreen screen;
        [SerializeField] ApplicantProfileAsset profile;
        [SerializeField] int seed = 1201;
        [SerializeField, Min(1)] int productionDays = 15;
        [SerializeField] TutorialController controller;
        [SerializeField] TutorialOverlay overlay;

        void Awake()
        {
            try
            {
                if (!screenWrapper || screenWrapper.activeSelf || !screen || !profile || !controller || !overlay || !overlay.HasAuthoredUi)
                    throw new InvalidOperationException("Tutorial requires an inactive Screen wrapper and authored bindings.");
                if (!string.Equals(profile.characterId, "character-01", StringComparison.Ordinal))
                    throw new InvalidOperationException("Tutorial demo must use character-01.");
                screen.SetSessionHost(new TutorialInterviewSessionHost(profile, seed, productionDays));
                controller.Bind(screen, overlay);
                screenWrapper.SetActive(true);
                controller.Begin();
            }
            catch (Exception error)
            {
                Debug.LogError("Tutorial bootstrap failed: " + error.Message, this);
                if (screen) screen.DisposeSession();
                if (controller && overlay) controller.ShowError(overlay, error.Message);
            }
        }

        void OnDestroy() { if (screen) screen.DisposeSession(); }

#if UNITY_EDITOR
        public void RefreshEditor(ApplicantProfileAsset demo, Sprite idle)
        {
            if (!screenWrapper || !screen || !controller || !overlay || !overlay.HasAuthoredUi
                || !screen.transform.IsChildOf(screenWrapper.transform)
                || !UnityEditor.PrefabUtility.IsPartOfPrefabInstance(screen))
                throw new InvalidOperationException("Existing TutorialScene has incomplete or disconnected authored bindings.");
            screenWrapper.SetActive(false);
            profile = demo;
            screen.ConfigureTutorialTargetsEditor();
            overlay.SetSupervisorIdleEditor(idle);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ConfigureEditor(GameObject wrapper, InterviewScreen view, ApplicantProfileAsset demo,
            TutorialController flow, TutorialOverlay mask)
        {
            screenWrapper = wrapper; screen = view; profile = demo; controller = flow; overlay = mask;
        }
#endif
    }
}
