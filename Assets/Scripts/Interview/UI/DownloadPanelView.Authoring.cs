#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed partial class DownloadPanelView
    {
        const string NativeArt = "Assets/Art/Discord/Source/Aseprite/디코 통화방 UI/";
        static void RequireEditor() { if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Download authoring requires EditMode."); }
        public static Sprite LoadDownloadSpriteEditor(string name)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(NativeArt + name + ".aseprite").OfType<Sprite>()
                .Where(s => s.name == name && s.rect.width == 13 && s.rect.height == 13).ToArray();
            if (sprites.Length != 1) throw new InvalidOperationException("Expected one native 13x13 Sprite: " + name);
            return sprites[0];
        }
        public void PreflightItemsEditor()
        {
            RequireEditor();
            LoadDownloadSpriteEditor("download"); LoadDownloadSpriteEditor("Check");
            if (_items != null && _items.Length != 0)
            {
                if (!HasAuthoredUi) throw new InvalidOperationException("Partial/duplicate download item configuration.");
                foreach (var item in _items)
                {
                    var so = new SerializedObject(item);
                    var fill = so.FindProperty("_fill").objectReferenceValue as Image;
                    var icon = so.FindProperty("_statusIcon").objectReferenceValue as Image;
                    if (!fill || !icon || !fill.transform.IsChildOf(item.Button.transform) || !icon.transform.IsChildOf(item.Button.transform))
                        throw new InvalidOperationException("Item Fill/icon must belong to its Button.");
                    if (so.FindProperty("_downloadSprite").objectReferenceValue != LoadDownloadSpriteEditor("download") ||
                        so.FindProperty("_checkSprite").objectReferenceValue != LoadDownloadSpriteEditor("Check"))
                        throw new InvalidOperationException("Item native download/Check binding mismatch.");
                    if (item.Button.GetComponents<DownloadItemView>().Length != 1 || item.gameObject != item.Button.gameObject)
                        throw new InvalidOperationException("Expected one item on its own Button.");
                }
                return;
            }
            if (!_panelRect || !_closeButton || !_applicationButton || !_portfolioButton || _applicationButton == _portfolioButton)
                throw new InvalidOperationException("Legacy download panel references missing.");
            if (!(_minimumDownloadSeconds >= .2f && _maximumDownloadSeconds <= 1f && _maximumDownloadSeconds >= _minimumDownloadSeconds))
                throw new InvalidOperationException("Invalid legacy download duration.");
            CheckLegacy(_applicationButton, _applicationFill); CheckLegacy(_portfolioButton, _portfolioFill);
        }
        void CheckLegacy(Button button, Image fill)
        {
            if (!button.transform.IsChildOf(transform) || button.GetComponents<DownloadItemView>().Length != 0)
                throw new InvalidOperationException("Partial item configuration or foreign Button.");
            if (fill && !fill.transform.IsChildOf(button.transform)) throw new InvalidOperationException("Foreign progress Fill.");
            foreach (string name in new[] { "ProgressFill", "SkinDownloadIcon" })
            {
                var children = Enumerable.Range(0, button.transform.childCount).Select(button.transform.GetChild).Where(t => t.name == name).ToArray();
                if (children.Length > 1 || (children.Length == 1 && (!(children[0] is RectTransform) || !children[0].GetComponent<Image>())))
                    throw new InvalidOperationException("Invalid named download child: " + name);
            }
        }
        public void EnsureItemsEditor()
        {
            PreflightItemsEditor();
            if (_items != null && _items.Length != 0) return;
            var download = LoadDownloadSpriteEditor("download"); var check = LoadDownloadSpriteEditor("Check");
            Undo.RecordObject(this, "Configure download items");
            _items = new[] {
                CreateItem(_applicationButton, _applicationFill, DocumentKind.Application, download, check),
                CreateItem(_portfolioButton, _portfolioFill, DocumentKind.Portfolio, download, check) };
            if (!HasAuthoredUi) throw new InvalidOperationException("Download item authoring incomplete.");
            Record(this);
        }
        DownloadItemView CreateItem(Button button, Image fill, DocumentKind kind, Sprite download, Sprite check)
        {
            if (!fill) fill = EnsureImage(button, "ProgressFill", true);
            var icon = EnsureImage(button, "SkinDownloadIcon", false);
            Undo.RecordObject(icon, "Configure download icon");
            icon.sprite = download;
            Record(icon);
            var item = Undo.AddComponent<DownloadItemView>(button.gameObject);
            item.ConfigureEditor(kind, button, fill, icon, download, check, _minimumDownloadSeconds, _maximumDownloadSeconds);
            Record(item);
            return item;
        }
        static Image EnsureImage(Button button, string name, bool fill)
        {
            var child = button.transform.Find(name);
            if (child) return child.GetComponent<Image>();
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Configure download items");
            Undo.SetTransformParent(go.transform, button.transform, "Configure download items");
            var image = go.GetComponent<Image>(); var rect = image.rectTransform;
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            rect.anchorMin = fill ? Vector2.zero : new Vector2(.89f, .36f);
            rect.anchorMax = fill ? new Vector2(0, 1) : new Vector2(.96f, .64f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            image.raycastTarget = false;
            image.color = fill ? new Color(.2f, .75f, .35f, 1) : Color.white;
            image.preserveAspect = !fill;
            if (fill) rect.SetSiblingIndex(0);
            Record(image); Record(rect);
            return image;
        }
        static void Record(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        public void Configure(RectTransform panel, Button application, Button portfolio, Button close, Vector2 closedPosition, Vector2 openPosition, float duration)
        {
            RequireEditor();
            if (_items != null && _items.Length != 0)
            {
                PreflightItemsEditor();
                if (_panelRect != panel || _closeButton != close || !_items.Any(i => i.Kind == DocumentKind.Application && i.Button == application) ||
                    !_items.Any(i => i.Kind == DocumentKind.Portfolio && i.Button == portfolio)) throw new InvalidOperationException("Cannot replace configured download references.");
                return;
            }
            Undo.RecordObject(this, "Configure download panel");
            _panelRect = panel; _applicationButton = application; _portfolioButton = portfolio; _closeButton = close;
            _closedPosition = closedPosition; _openPosition = openPosition; _slideSeconds = duration;
            EnsureItemsEditor();
        }
        public void ConfigureProgressEditor(Image applicationFill, Image portfolioFill)
        {
            RequireEditor();
            if (_items != null && _items.Length != 0)
            {
                PreflightItemsEditor();
                foreach (var item in _items)
                {
                    var expected = item.Kind == DocumentKind.Application ? applicationFill : portfolioFill;
                    if (new SerializedObject(item).FindProperty("_fill").objectReferenceValue != expected)
                        throw new InvalidOperationException("Configured item Fill cannot be replaced.");
                }
            }
            Undo.RecordObject(this, "Configure download progress");
            _applicationFill = applicationFill; _portfolioFill = portfolioFill;
            Record(this);
        }
    }
}
#endif
