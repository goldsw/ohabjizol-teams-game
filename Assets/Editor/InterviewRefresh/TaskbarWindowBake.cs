#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ohabjizol.UI;

namespace Ohabjizol.Interview
{
    public static class TaskbarWindowBake
    {
        public const string ItemPath = DocumentsUiBake.Folder + "/ManagedWindowTaskbarItem.prefab";
        public static void BuildAssets()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Taskbar bake requires EditMode.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ItemPath)) return;
            Font font = AssetDatabase.LoadAssetAtPath<Font>(DocumentsUiBake.FontPath);
            if (!font) throw new InvalidOperationException("Taskbar font missing.");
            GameObject host = new GameObject("ManagedWindowTaskbarItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ManagedWindowTaskbarItem));
            try
            {
                RectTransform rect = (RectTransform)host.transform;
                rect.sizeDelta = new Vector2(168, 52);
                LayoutElement sizing = host.GetComponent<LayoutElement>();
                sizing.minWidth = sizing.preferredWidth = 168;
                sizing.minHeight = sizing.preferredHeight = 52;
                Image surface = host.GetComponent<Image>();
                surface.color = new Color32(166, 159, 152, 255);
                Button button = host.GetComponent<Button>();
                button.targetGraphic = surface;
                button.transition = Selectable.Transition.None;
                Text glyph = UiKit.Text(rect, "IconText", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
                glyph.text = "PDF";
                Pin(glyph.rectTransform, 4, 44);
                Image icon = UiKit.Image(rect, "Icon", Color.white);
                Pin(icon.rectTransform, 8, 40);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.gameObject.SetActive(false);
                Text title = UiKit.Text(rect, "Title", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
                title.text = "문서.pdf";
                Pin(title.rectTransform, 48, 164);
                foreach (Text text in host.GetComponentsInChildren<Text>(true))
                { text.font = font; text.color = new Color32(43, 31, 24, 255); text.raycastTarget = false; }
                host.GetComponent<ManagedWindowTaskbarItem>().ConfigureEditor(button, title, glyph, icon, surface);
                PrefabUtility.SaveAsPrefabAsset(host, ItemPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        static void Pin(RectTransform rect, float left, float right)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(0, 1);
            rect.offsetMin = new Vector2(left, 4); rect.offsetMax = new Vector2(right, -4);
        }
        public static void Apply(string prefabPath)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Taskbar migration requires EditMode.");
            DocumentsUiBake.BuildAssets();
            BuildAssets();
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPath);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                InterviewScreen screen = root.GetComponentInChildren<InterviewScreen>(true);
                if (!screen) throw new InvalidOperationException("InterviewScreen missing: " + prefabPath);
                screen.BakeManagedWindowTaskbarUi(asset.GetComponent<ManagedWindowTaskbarItem>());
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
