using Ohabjizol.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>Single-click desktop shortcut to the credits scene.</summary>
    public sealed class DesktopCreditsButton : MonoBehaviour
    {
        [SerializeField] DesktopView desktop;
        [SerializeField] Button button;
        bool opening;

        public bool HasAuthoredUi => desktop && button;

        void OnEnable()
        {
            opening = false;
            if (button) button.onClick.AddListener(OpenCredits);
        }

        void OnDisable()
        {
            if (button) button.onClick.RemoveListener(OpenCredits);
        }

        public void OpenCredits()
        {
            if (opening || !HasAuthoredUi || !desktop.CanActivateApps || MainMenuTitleOverlay.IsBlocking) return;
            opening = true;
            desktop.ClearSelection();
            CreditsSceneFlow.Open();
        }

#if UNITY_EDITOR
        public void ConfigureEditor(DesktopView view, Button creditsButton)
        {
            desktop = view;
            button = creditsButton;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
