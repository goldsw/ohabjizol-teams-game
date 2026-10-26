using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ohabjizol.Tutorial
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1100)]
    public sealed class MainMenuTitleOverlay : MonoBehaviour
    {
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private Image backdrop;
        [SerializeField] private Text prompt;

        private static bool seen;
        private static MainMenuTitleOverlay active;
        private static int blockedThroughFrame = -1;
        private bool showing;
        private bool dismissing;

        public static event System.Action Dismissed;

        public static bool IsBlocking =>
            StartupLogoSequence.IsBlocking || (active != null && active.showing && !active.dismissing)
            || Time.frameCount <= blockedThroughFrame;

        public bool HasAuthoredUi => overlayCanvas != null && backdrop != null && prompt != null
            && overlayCanvas.gameObject == gameObject
            && overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            && overlayCanvas.sortingOrder >= 2000
            && GetComponent<CanvasScaler>() != null && GetComponent<GraphicRaycaster>() != null
            && backdrop.transform.IsChildOf(transform) && backdrop.raycastTarget
            && prompt.transform.IsChildOf(transform) && prompt.font != null
            && prompt.font == Ohabjizol.UI.UiKit.Font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            seen = false;
            active = null;
            blockedThroughFrame = -1;
            Dismissed = null;
        }

        public static void SuppressForSession()
        {
            seen = true;
            if (active == null) return;
            active.showing = false;
            active.dismissing = false;
            active.SetVisible(false);
            active = null;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            showing = false;
            dismissing = false;
            if (seen || !HasAuthoredUi)
            {
                SetVisible(false);
                return;
            }

            seen = true;
            active = this;
            showing = true;
            SetVisible(true);
            prompt.enabled = true;
        }

        private void Update()
        {
            if (!showing || StartupLogoSequence.IsBlocking)
                return;

            if (dismissing)
            {
                if (Time.frameCount > blockedThroughFrame)
                {
                    showing = false;
                    SetVisible(false);
                    if (active == this)
                        active = null;
                    Dismissed?.Invoke();
                }
                return;
            }

            Color color = prompt.color;
            color.a = Mathf.Lerp(0.2f, 1f, (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) + 1f) * 0.5f);
            prompt.color = color;
            if (!HasStartInput())
                return;

            Ohabjizol.Presentation.UiSoundPlayer.PlayClick();
            // Keep the raycast shield as well as the public guard through the next frame.
            blockedThroughFrame = Time.frameCount + 1;
            dismissing = true;
            prompt.enabled = false;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            if (active == this)
                active = null;
            showing = false;
            dismissing = false;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (overlayCanvas) overlayCanvas.enabled = visible;
            if (backdrop) backdrop.gameObject.SetActive(visible);
            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster) raycaster.enabled = visible;
        }

        private static bool HasStartInput()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame
                || mouse.forwardButton.wasPressedThisFrame || mouse.backButton.wasPressedThisFrame))
                return true;
            foreach (var gamepad in Gamepad.all)
                foreach (var control in gamepad.allControls)
                    if (control is ButtonControl button && button.wasPressedThisFrame)
                        return true;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.wasPressedThisFrame)
                        return true;
            return false;
        }

#if UNITY_EDITOR
        public static MainMenuTitleOverlay CreateEditor(Transform parent)
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("Author the title overlay outside Play Mode.");
            if (parent == null)
                throw new System.ArgumentNullException(nameof(parent));

            // Resolve the imported font before changing the hierarchy.
            Font font = AssetDatabase.LoadAssetAtPath<Font>(Ohabjizol.UI.UiKit.FontAssetPath);
            if (font == null)
                throw new System.InvalidOperationException("Import PFStardustBold.ttf before authoring the title overlay.");

            MainMenuTitleOverlay overlay = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                overlay = parent.GetChild(i).GetComponent<MainMenuTitleOverlay>();
                if (overlay != null)
                    break;
            }
            GameObject root = overlay != null ? overlay.gameObject : Child(parent, "MainMenuTitleOverlay");
            if (overlay == null)
                overlay = Ensure<MainMenuTitleOverlay>(root);
            Undo.RecordObject(overlay, "Author title overlay");
            overlay.overlayCanvas = Ensure<Canvas>(root);
            Undo.RecordObject(overlay.overlayCanvas, "Configure title canvas");
            overlay.overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.overlayCanvas.overrideSorting = true;
            overlay.overlayCanvas.sortingOrder = 2000;
            overlay.overlayCanvas.enabled = true;
            var scaler = Ensure<CanvasScaler>(root);
            Undo.RecordObject(scaler, "Configure title scaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Ensure<GraphicRaycaster>(root);

            overlay.backdrop = Ensure<Image>(Child(root.transform, "Backdrop"));
            Undo.RecordObject(overlay.backdrop, "Configure title backdrop");
            overlay.backdrop.color = Color.black;
            overlay.backdrop.raycastTarget = true;
            Stretch(overlay.backdrop.rectTransform);
            overlay.prompt = Ensure<Text>(Child(overlay.backdrop.transform, "Prompt"));
            Undo.RecordObject(overlay.prompt, "Configure title prompt");
            overlay.prompt.font = font;
            overlay.prompt.text = "화면 터치하여 시작";
            overlay.prompt.fontSize = 48;
            overlay.prompt.alignment = TextAnchor.MiddleCenter;
            overlay.prompt.color = Color.white;
            overlay.prompt.raycastTarget = false;
            overlay.prompt.supportRichText = false;
            overlay.prompt.enabled = true;
            Stretch(overlay.prompt.rectTransform);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                EditorUtility.SetDirty(component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            return overlay;
        }

        private static GameObject Child(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;
            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create title UI");
            Undo.SetTransformParent(child.transform, parent, "Parent title UI");
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static T Ensure<T>(GameObject target) where T : Component
        {
            var existing = target.GetComponent<T>();
            if (existing) return existing;
            var added = Undo.AddComponent<T>(target);
            if (!added)
                throw new System.InvalidOperationException("Could not author " + typeof(T).Name + " on " + target.name);
            return added;
        }

        private static void Stretch(RectTransform rect)
        {
            Undo.RecordObject(rect, "Stretch title UI");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
#endif
    }
}
