using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewTeamSlotView : MonoBehaviour
    {
        [SerializeField] Image _portrait;
        [SerializeField, InspectorName("Profile Background")] Image _placeholder;
        [SerializeField] Mask _profileMask;
        [SerializeField, Min(.01f)] float _faceScale = 2.2f;
        [SerializeField] Vector2 _faceOffset = new Vector2(0, -18);
        [SerializeField, HideInInspector] bool _topAnchoredFaceReductionApplied;
        [SerializeField, HideInInspector] bool _profileBackgroundSeparated;

        const float FaceReduction = 2f / 3f;
        static readonly Vector2 FacePivot = new Vector2(.5f, 1f);

        public bool HasAuthoredPortrait => _portrait && _placeholder && _profileMask &&
            _profileMask.graphic && _profileMask.graphic is Image maskImage && maskImage.sprite &&
            !_profileMask.showMaskGraphic && _portrait.maskable && _portrait != _placeholder &&
            _placeholder != maskImage && _portrait != maskImage &&
            _portrait.transform.parent == _profileMask.transform &&
            (_placeholder.transform.parent == transform && _profileMask.transform.parent == transform ||
             _profileMask.transform.parent && _profileMask.transform.parent.parent == transform &&
             _profileMask.transform.parent.name == "Profile" &&
             _profileMask.transform.parent is RectTransform &&
             _placeholder.transform.parent == _profileMask.transform.parent);

        public void BindPortrait(SelectedApplicantDisplay member, ApplicantVisualCatalog catalog)
            => BindCharacterPortrait(member == null ? null : member.CharacterId, catalog);

        public void BindCharacterPortrait(string characterId, ApplicantVisualCatalog catalog)
        {
            if (!HasAuthoredPortrait)
                throw new InvalidOperationException("Team slot portrait/mask/placeholder references are incomplete.");
            _portrait.sprite = null;
            _portrait.enabled = false;
            _placeholder.gameObject.SetActive(true);
            ApplyCrop();
            if (string.IsNullOrEmpty(characterId) || !catalog || !catalog.TryGet(characterId, out var visual) || !visual.Idle)
                return;
            _portrait.sprite = visual.Idle;
            _portrait.enabled = true;
            _placeholder.gameObject.SetActive(_profileBackgroundSeparated);
        }

        void ApplyCrop()
        {
            if (!_portrait) return;
            if (_topAnchoredFaceReductionApplied)
                _portrait.rectTransform.pivot = FacePivot;
            _portrait.rectTransform.localScale = Vector3.one *
                (_topAnchoredFaceReductionApplied ? _faceScale : Mathf.Max(.1f, _faceScale));
            _portrait.rectTransform.anchoredPosition = _faceOffset;
            // Existing serialized crops are migrated explicitly in the editor,
            // or on first runtime bind when the scene has not been migrated yet.
            if (Application.isPlaying)
                ReduceFaceFromTop();
        }

        bool ReduceFaceFromTop()
        {
            if (!_portrait || _topAnchoredFaceReductionApplied) return false;
            var rect = _portrait.rectTransform;
            var top = new Vector3(rect.rect.center.x, rect.rect.yMax, 0);
            Vector3 worldTop = rect.TransformPoint(top);
            rect.pivot = FacePivot;
            _faceScale = Mathf.Max(.1f, _faceScale) * FaceReduction;
            rect.localScale = Vector3.one * _faceScale;
            rect.position += worldTop - rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.yMax, 0));
            _faceOffset = rect.anchoredPosition;
            _topAnchoredFaceReductionApplied = true;
            return true;
        }

        void OnValidate() { ApplyCrop(); }

#if UNITY_EDITOR
        public bool SeparateProfileBackgroundEditor()
        {
            if (Application.isPlaying || !HasAuthoredPortrait)
                throw new InvalidOperationException("Separate complete authored profiles outside Play mode.");
            bool changed = !_profileBackgroundSeparated;
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Separate profile background");
            var root = _profileMask.transform.parent as RectTransform;
            if (root == transform)
            {
                if (transform.Find("Profile"))
                    throw new InvalidOperationException("Existing Profile has unexpected references.");
                var obj = new GameObject("Profile", typeof(RectTransform), typeof(LayoutElement));
                UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Create profile cell");
                root = (RectTransform)obj.transform;
                root.SetParent(transform, false);
                root.SetSiblingIndex(_profileMask.transform.GetSiblingIndex());
                PlaceProfileEditor(root);
                UnityEditor.Undo.SetTransformParent(_placeholder.transform, root, "Move profile background");
                UnityEditor.Undo.SetTransformParent(_profileMask.transform, root, "Move profile mask");
                changed = true;
            }
            root.localScale = Vector3.one;
            var size = root.GetComponent<LayoutElement>();
            if (!size) size = UnityEditor.Undo.AddComponent<LayoutElement>(root.gameObject);
            size.ignoreLayout = false;
            size.minWidth = size.preferredWidth = 56;
            size.minHeight = size.preferredHeight = 56;
            size.flexibleWidth = size.flexibleHeight = 0;
            _placeholder.gameObject.name = "ProfileBackground";
            _placeholder.transform.SetAsFirstSibling();
            foreach (var rect in new[] { _placeholder.rectTransform, (RectTransform)_profileMask.transform })
            {
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                var overlay = rect.GetComponent<LayoutElement>();
                if (overlay) overlay.ignoreLayout = true;
                rect.gameObject.SetActive(true);
            }
            _profileMask.showMaskGraphic = false;
            if (!_profileBackgroundSeparated)
            {
                _faceOffset.y += 8;
                _placeholder.sprite = ((Image)_profileMask.graphic).sprite;
                _placeholder.overrideSprite = null;
                _placeholder.type = Image.Type.Simple;
                _placeholder.color = new Color(.12f, .13f, .15f, 1);
                _placeholder.preserveAspect = true;
                _placeholder.raycastTarget = false;
            }
            _placeholder.enabled = true;
            _profileBackgroundSeparated = true;
            ApplyCrop();
            foreach (var component in GetComponentsInChildren<Component>(true))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                UnityEditor.EditorUtility.SetDirty(component);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
                UnityEditor.EditorUtility.SetDirty(component.gameObject);
            }
            return changed;
        }

        /// <summary>Migrates this profile's serialized crop once; returns true when changed.</summary>
        public bool MigrateProfilePhotoScaleEditor()
        {
            if (_topAnchoredFaceReductionApplied) return false;
            if (!HasAuthoredPortrait)
                throw new InvalidOperationException("Team slot portrait/mask/placeholder references are incomplete.");
            if (Application.isPlaying)
                throw new InvalidOperationException("Migrate profile photos outside Play mode.");
            var rect = _portrait.rectTransform;
            UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { this, rect }, "Reduce profile photo from top");
            ApplyCrop();
            ReduceFaceFromTop();
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(rect);
            return true;
        }

        internal static void PreflightEditor(Text label, Transform team, int index)
        {
            if (!label) throw new InvalidOperationException("Missing existing Slot" + index + " Text reference.");
            Transform group = team.Find("SlotGroup" + index);
            if (!group)
            {
                if (label.transform.parent != team)
                    throw new InvalidOperationException("Slot" + index + " has an unexpected parent.");
                return;
            }
            var slot = group.GetComponent<InterviewTeamSlotView>();
            if (!slot || !slot.HasAuthoredPortrait || (label.transform.parent != group && label.transform.parent != team))
                throw new InvalidOperationException("Partial SlotGroup" + index + "; repair its authored references before baking.");
        }

        internal static InterviewTeamSlotView AuthorEditor(Text label, Transform team, int index, Sprite profile, bool hideOriginal = true)
        {
            Transform existing = team.Find("SlotGroup" + index);
            if (existing)
            {
                UnityEditor.Undo.RegisterFullObjectHierarchyUndo(existing.gameObject, "Update existing profile slot");
                InterviewScreen.AuthorSlotCaptionEditor(label, existing, hideOriginal);
                var existingSlot = existing.GetComponent<InterviewTeamSlotView>();
                existingSlot.MigrateProfilePhotoScaleEditor();
                existingSlot.SeparateProfileBackgroundEditor();
                return existingSlot;
            }
            var original = label.rectTransform;
            var groupObject = new GameObject("SlotGroup" + index, typeof(RectTransform), typeof(InterviewTeamSlotView));
            UnityEditor.Undo.RegisterCreatedObjectUndo(groupObject, "Create team profile slot");
            var group = (RectTransform)groupObject.transform;
            group.SetParent(team, false);
            group.SetSiblingIndex(original.GetSiblingIndex());
            group.anchorMin = original.anchorMin; group.anchorMax = original.anchorMax;
            group.pivot = original.pivot; group.sizeDelta = original.sizeDelta;
            group.anchoredPosition3D = original.anchoredPosition3D;
            group.localRotation = original.localRotation; group.localScale = original.localScale;
            InterviewScreen.AuthorSlotCaptionEditor(label, group, hideOriginal);
            var slot = groupObject.GetComponent<InterviewTeamSlotView>();
            var maskImage = CreateImageEditor(group, "ProfileMask", profile);
            PlaceProfileEditor(maskImage.rectTransform);
            slot._profileMask = UnityEditor.Undo.AddComponent<Mask>(maskImage.gameObject);
            slot._profileMask.showMaskGraphic = false;
            slot._portrait = CreateImageEditor(maskImage.transform, "Portrait", null);
            slot._portrait.rectTransform.anchorMin = Vector2.zero;
            slot._portrait.rectTransform.anchorMax = Vector2.one;
            slot._portrait.rectTransform.offsetMin = slot._portrait.rectTransform.offsetMax = Vector2.zero;
            slot._portrait.preserveAspect = true; slot._portrait.maskable = true;
            slot._placeholder = CreateImageEditor(group, "Placeholder", profile);
            PlaceProfileEditor(slot._placeholder.rectTransform);
            slot.MigrateProfilePhotoScaleEditor();
            slot.SeparateProfileBackgroundEditor();
            slot.BindPortrait(null, null);
            foreach (var component in groupObject.GetComponentsInChildren<Component>(true))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                UnityEditor.EditorUtility.SetDirty(component);
            }
            return slot;
        }

        static Image CreateImageEditor(Transform parent, string name, Sprite sprite)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Create team profile image");
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false; image.material = null;
            return image;
        }

        static void PlaceProfileEditor(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(56, 56); rect.anchoredPosition = Vector2.zero;
        }
#endif
    }
}
