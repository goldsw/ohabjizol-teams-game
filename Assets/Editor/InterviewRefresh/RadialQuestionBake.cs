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
                var center = binding.FindProperty("_portrait").objectReferenceValue as RectTransform;
                if (!center || center == call.PortraitRect || center.name != "ApplicantCenter")
                    center = window ? window.transform.Find("Info/ApplicantCenter") as RectTransform : null;
                if (!center || center == call.PortraitRect)
                {
                    center = Rect("ApplicantCenter", windowParent);
                    center.anchorMin = center.anchorMax = center.pivot = new Vector2(.5f, .5f);
                    center.sizeDelta = Vector2.zero;
                    center.position = call.PortraitRect.TransformPoint(call.PortraitRect.rect.center);
                }
                center.SetParent(windowParent, true);
                if (!options) options = Rect("OptionsRoot", windowParent);
                if (options.parent != windowParent) options.SetParent(windowParent, false);
                options.localScale = Vector3.one;
                options.localRotation = Quaternion.identity;
                Stretch(options);
                options.anchoredPosition3D = Vector3.zero;
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
                fixedLauncher.ConfigureEditor(launcher, null, radial);
                launcher.interactable = true;
                launcher.gameObject.SetActive(true);
                var appsSize = fixedApps.GetComponent<LayoutElement>();
                if (!appsSize) appsSize = fixedApps.gameObject.AddComponent<LayoutElement>();
                float requiredWidth = fixedApps.childCount * 168f + Mathf.Max(0, fixedApps.childCount - 1) * 8f;
                appsSize.minWidth = Mathf.Max(appsSize.minWidth, requiredWidth);
                appsSize.preferredWidth = Mathf.Max(appsSize.preferredWidth, requiredWidth);
                appsSize.minHeight = Mathf.Max(appsSize.minHeight, 52f);
                appsSize.preferredHeight = Mathf.Max(appsSize.preferredHeight, 52f);
                if (legacyLauncher && legacyLauncher != launcher) legacyLauncher.gameObject.SetActive(false);
                radial.ConfigureWindowEditor(null, fixedLauncher);
                radial.ConfigureEditor(null, options, center, buttons);
                if (window) window.gameObject.SetActive(false);
                serialized.FindProperty("_radialQuestions").objectReferenceValue = radial;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(screen);
                EditorUtility.SetDirty(radial);
                if (window) EditorUtility.SetDirty(window);
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
