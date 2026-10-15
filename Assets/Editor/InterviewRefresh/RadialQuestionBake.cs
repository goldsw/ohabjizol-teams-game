using System;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class RadialQuestionBake
    {
        // Scoped migration only: never invoke the screen/document/full UI bakes here.
        public static void Apply(string prefabPath)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Question migration requires EditMode.");
            if (string.IsNullOrWhiteSpace(prefabPath)) throw new ArgumentException("Prefab path required.", nameof(prefabPath));
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var screen = prefab.GetComponentInChildren<InterviewScreen>(true);
                if (!screen) throw new InvalidOperationException("InterviewScreen missing.");
                var serialized = new SerializedObject(screen);
                var call = serialized.FindProperty("_call").objectReferenceValue as InterviewCallView;
                var windowParent = serialized.FindProperty("_windowParent").objectReferenceValue as RectTransform;
                var itemParent = serialized.FindProperty("_itemParent").objectReferenceValue as RectTransform;
                var source = serialized.FindProperty("_questions");
                if (!call || !call.PortraitRect || !windowParent || !itemParent || source.arraySize != 5)
                    throw new InvalidOperationException("Authored portrait, window/taskbar parents and five questions required.");
                var buttons = new Button[5];
                for (int i = 0; i < buttons.Length; i++)
                {
                    var view = source.GetArrayElementAtIndex(i).objectReferenceValue as InterviewQuestionView;
                    if (!view || !view.Button) throw new InvalidOperationException("Question button missing at index " + i);
                    buttons[i] = view.Button;
                }
                var radial = serialized.FindProperty("_radialQuestions").objectReferenceValue as RadialQuestionView;
                bool newRadial = !radial;
                if (newRadial)
                {
                    RectTransform controller = Rect("RadialQuestions", windowParent);
                    Stretch(controller);
                    radial = controller.gameObject.AddComponent<RadialQuestionView>();
                }
                var binding = new SerializedObject(radial);
                var options = binding.FindProperty("_optionsRoot").objectReferenceValue as RectTransform;
                var legacyLauncher = binding.FindProperty("_launcher").objectReferenceValue as Button;
                var window = binding.FindProperty("_window").objectReferenceValue as RadialQuestionWindow;
                if (!window)
                {
                    Transform existing = windowParent.Find("RadialQuestionWindow");
                    if (existing) window = existing.GetComponent<RadialQuestionWindow>();
                }
                bool newWindow = !window;
                if (newWindow)
                {
                    float radius = binding.FindProperty("_radius").floatValue;
                    float halfWidth = 77f, halfHeight = 24f;
                    foreach (Button button in buttons)
                    {
                        Rect rect = ((RectTransform)button.transform).rect;
                        halfWidth = Mathf.Max(halfWidth, rect.width * .5f);
                        halfHeight = Mathf.Max(halfHeight, rect.height * .5f);
                    }
                    Vector2 size = new Vector2(2f * (radius + halfWidth + 24f),
                        2f * (radius + halfHeight + 24f) + WindowToolbar.ToolbarHeight);
                    window = WindowFactory.Create<RadialQuestionWindow>(windowParent, "질문", size, Vector2.zero);
                    window.name = "RadialQuestionWindow";
                    // Transparent info leaves the applicant visible through the question ring.
                    Image infoImage = window.Info.GetComponent<Image>();
                    if (infoImage) { infoImage.color = Color.clear; infoImage.raycastTarget = false; }
                    Vector3 portraitCenter = call.PortraitRect.TransformPoint(call.PortraitRect.rect.center);
                    window.Root.position = portraitCenter + window.Root.TransformVector(new Vector3(0f, WindowToolbar.ToolbarHeight * .5f, 0f));
                    AddMinimize(window);
                    window.CaptureEditorReferences();
                }
                else window.InitializeAuthored();
                Image rootImage = window.GetComponent<Image>();
                if (rootImage) { rootImage.color = Color.clear; rootImage.raycastTarget = false; }
                Image authoredInfoImage = window.Info ? window.Info.GetComponent<Image>() : null;
                if (authoredInfoImage) { authoredInfoImage.color = Color.clear; authoredInfoImage.raycastTarget = false; }
                if (!window.Info) throw new InvalidOperationException("Question window Info reference missing.");
                if (!options) options = Rect("OptionsRoot", window.Info);
                if (options.parent != window.Info) options.SetParent(window.Info, true);
                Stretch(options);
                Transform centerTransform = window.Info.Find("ApplicantCenter");
                RectTransform center = centerTransform as RectTransform;
                if (!center)
                {
                    center = Rect("ApplicantCenter", window.Info);
                    center.anchorMin = center.anchorMax = center.pivot = new Vector2(.5f, .5f);
                    center.anchoredPosition = Vector2.zero;
                    center.sizeDelta = Vector2.zero;
                }
                foreach (Button button in buttons)
                {
                    if (button.transform.parent != options) button.transform.SetParent(options, true);
                    var layout = button.GetComponent<LayoutElement>();
                    if (layout) layout.ignoreLayout = true;
                }
                Transform fixedTransform = itemParent.Find("FixedApps");
                RectTransform fixedApps = fixedTransform as RectTransform;
                if (!fixedApps)
                {
                    fixedApps = Rect("FixedApps", itemParent);
                    fixedApps.SetAsFirstSibling();
                    var group = fixedApps.gameObject.AddComponent<HorizontalLayoutGroup>();
                    group.spacing = 8f;
                    group.childAlignment = TextAnchor.MiddleLeft;
                    group.childControlWidth = group.childControlHeight = true;
                    group.childForceExpandWidth = group.childForceExpandHeight = false;
                }
                Transform fixedButton = fixedApps.Find("RadialQuestionLauncher");
                Button launcher = fixedButton ? fixedButton.GetComponent<Button>() : null;
                if (!launcher)
                {
                    launcher = UiKit.Button(fixedApps, "RadialQuestionLauncher", "질문", new Color32(240, 158, 74, 255), 18);
                    ((RectTransform)launcher.transform).sizeDelta = new Vector2(168f, 52f);
                    var size = launcher.gameObject.AddComponent<LayoutElement>();
                    size.minWidth = size.preferredWidth = 168f;
                    size.minHeight = size.preferredHeight = 52f;
                    Text original = buttons[0].GetComponentInChildren<Text>(true);
                    foreach (Text label in launcher.GetComponentsInChildren<Text>(true))
                    { if (original && original.font) label.font = original.font; label.raycastTarget = false; }
                }
                var fixedLauncher = launcher.GetComponent<RadialQuestionLauncher>();
                if (!fixedLauncher) fixedLauncher = launcher.gameObject.AddComponent<RadialQuestionLauncher>();
                fixedLauncher.ConfigureEditor(launcher, window);
                launcher.interactable = true;
                launcher.gameObject.SetActive(true);
                var appsSize = fixedApps.GetComponent<LayoutElement>();
                if (!appsSize) appsSize = fixedApps.gameObject.AddComponent<LayoutElement>();
                float requiredWidth = fixedApps.childCount * 168f + Mathf.Max(0, fixedApps.childCount - 1) * 8f;
                appsSize.minWidth = Mathf.Max(appsSize.minWidth, requiredWidth);
                appsSize.preferredWidth = Mathf.Max(appsSize.preferredWidth, requiredWidth);
                appsSize.minHeight = Mathf.Max(appsSize.minHeight, 52f);
                appsSize.preferredHeight = Mathf.Max(appsSize.preferredHeight, 52f);
                radial.ConfigureWindowEditor(window, fixedLauncher);
                radial.ConfigureEditor(legacyLauncher, options, center, buttons);
                window.ConfigureEditor(radial);
                if (newWindow) window.gameObject.SetActive(false);
                serialized.FindProperty("_radialQuestions").objectReferenceValue = radial;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(screen);
                EditorUtility.SetDirty(radial);
                EditorUtility.SetDirty(window);
                EditorUtility.SetDirty(fixedLauncher);
                if (!PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath))
                    throw new InvalidOperationException("Failed to save radial question prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static void AddMinimize(RadialQuestionWindow window)
        {
            window.Controls.sizeDelta = new Vector2(WindowToolbar.ToolbarHeight * 2f, 0f);
            RectTransform close = window.Controls.Find("Close") as RectTransform;
            close.anchorMin = new Vector2(.5f, 0f);
            close.anchorMax = Vector2.one;
            close.offsetMin = close.offsetMax = Vector2.zero;
            Button minimize = UiKit.Button(window.Controls, "Minimize", "-", new Color32(240, 158, 74, 255), 22);
            RectTransform rect = (RectTransform)minimize.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            ((RectTransform)window.Toolbar.Find("Drag")).offsetMax = new Vector2(-80f, 0f);
            ((RectTransform)window.Toolbar.Find("Title")).offsetMax = new Vector2(-80f, 0f);
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
