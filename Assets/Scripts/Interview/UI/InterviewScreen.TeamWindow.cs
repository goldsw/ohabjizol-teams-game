using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        [Header("Current team window")]
        [SerializeField] InterviewTeamWindow _teamWindow;
        [SerializeField] InterviewTeamLauncher _teamLauncher;
        public InterviewTeamWindow TeamWindow => _teamWindow;
#if UNITY_EDITOR
        public static Sprite LoadTeamProfileSpriteEditor()
        {
            const string path = "Assets/Art/Discord/Source/Aseprite/오합지졸-디코/profile.aseprite";
            Sprite profile = null;
            int count = 0;
            foreach (UnityEngine.Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is Sprite sprite) || sprite.name != "profile") continue;
                profile = sprite;
                count++;
            }
            if (count != 1)
                throw new InvalidOperationException(
                    "Team profile requires exactly one Sprite named 'profile' at " + path + "; found " + count + ".");
            return profile;
        }

        public void BakeTeamProfilesEditor()
        {
            if (Application.isPlaying || !_teamWindow || !_applicantStats)
                throw new InvalidOperationException("Team profiles require EditMode, an existing team window and ApplicantStats reference.");
            var profile = LoadTeamProfileSpriteEditor();
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ApplicantVisualCatalog>("Assets/Art/Applicants/ApplicantVisualCatalog.asset");
            if (!profile || !catalog)
                throw new InvalidOperationException("Team profiles require the native profile Sprite and ApplicantVisualCatalog.asset assets.");
            _teamWindow.InitializeAuthored();
            var team = _teamWindow.Info ? _teamWindow.Info.Find("TeamSummary") : null;
            if (!team || _slotLabels == null || _slotLabels.Length != 6 || !_currentLabel || !_requiredLabel)
                throw new InvalidOperationException("TeamSummary and its six existing slot/current/required Text references are required.");
            if (_currentLabel.transform.parent != team || _requiredLabel.transform.parent != team)
                throw new InvalidOperationException("Current/Required must remain children of TeamSummary.");
            var uniqueLabels = new System.Collections.Generic.HashSet<Text>();
            for (int i = 0; i < 6; i++)
            {
                if (!uniqueLabels.Add(_slotLabels[i]) || _slotLabels[i] == _currentLabel || _slotLabels[i] == _requiredLabel)
                    throw new InvalidOperationException("Team slot Text references must be distinct from each other and Current/Required.");
                InterviewTeamSlotView.PreflightEditor(_slotLabels[i], team, i);
            }
            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Bake team profiles");
            try
            {
                var infoImage = _teamWindow.Info.GetComponent<Image>();
                if (!infoImage)
                    throw new InvalidOperationException("Team window Info requires Image.");
                UnityEditor.Undo.RecordObject(infoImage, "Use solid team panel background");
                infoImage.sprite = null;
                infoImage.overrideSprite = null;
                infoImage.type = Image.Type.Simple;
                infoImage.color = new Color(.08f, .08f, .10f, 1);
                infoImage.preserveAspect = false;
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(infoImage);
                UnityEditor.EditorUtility.SetDirty(infoImage);
                var slots = new InterviewTeamSlotView[6];
                UnityEditor.Undo.RecordObject(this, "Reconnect team captions");
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = InterviewTeamSlotView.AuthorEditor(_slotLabels[i], team, i, profile);
                    _slotLabels[i] = slots[i].transform.Find("Caption").GetComponent<Text>();
                    AuthorRowLayoutEditor((RectTransform)slots[i].transform);
                }
                AuthorTeamLayoutEditor((RectTransform)team);
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                _teamWindow.ConfigureProfilesEditor(slots, catalog);
                UnityEditor.Undo.RecordObject(_applicantStats.gameObject, "Hide applicant stats");
                _applicantStats.gameObject.SetActive(false);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_applicantStats.gameObject);
                UnityEditor.EditorUtility.SetDirty(_applicantStats.gameObject);
                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        public void BakeTeamWindow()
        {
            if (Application.isPlaying || !_windowParent || !_itemParent)
                throw new InvalidOperationException("Team window requires authored workspace in EditMode.");
            if (_teamWindow) _teamWindow.InitializeAuthored();
            RectTransform team = _teamWindow ? _teamWindow.Info?.Find("TeamSummary") as RectTransform : null;
            if (!team && _discordViewRoot) team = _discordViewRoot.Find("TeamSummary") as RectTransform;
            if (!team) throw new InvalidOperationException("Existing TeamSummary required; summary references must be preserved.");
            if (!_teamWindow)
            {
                Vector3 center = _windowParent.InverseTransformPoint(team.TransformPoint(team.rect.center));
                Vector2 size = team.rect.size;
                if (size.x < 1 || size.y < 1) size = new Vector2(460, 760);
                _teamWindow = WindowFactory.Create<InterviewTeamWindow>(_windowParent, "현재 팀",
                    size + new Vector2(0, WindowToolbar.ToolbarHeight),
                    new Vector2(center.x, center.y + WindowToolbar.ToolbarHeight * .5f));
            }
            _teamWindow.InitializeAuthored();
            if (team.parent != _teamWindow.Info)
            {
                UnityEditor.Undo.SetTransformParent(team, _teamWindow.Info, "Move team summary");
                UnityEditor.Undo.RecordObject(team, "Place team summary");
                team.anchorMin = Vector2.zero; team.anchorMax = Vector2.one;
                team.offsetMin = team.offsetMax = Vector2.zero;
                team.localScale = Vector3.one;
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(team);
            }
            team.gameObject.SetActive(true);
            RectTransform controls = _teamWindow.Controls;
            controls.sizeDelta = new Vector2(WindowToolbar.ToolbarHeight * 2, 0);
            Button close = controls.Find("Close").GetComponent<Button>();
            UiKit.Place((RectTransform)close.transform, .5f, 0, 1, 1);
            Transform existingMinimize = controls.Find("Minimize");
            Button minimize = existingMinimize ? existingMinimize.GetComponent<Button>() :
                UiKit.Button(controls, "Minimize", "-", new Color(.28f, .29f, .32f), 20);
            UiKit.Place((RectTransform)minimize.transform, 0, 0, .5f, 1);
            RectTransform drag = _teamWindow.Toolbar.Find("Drag") as RectTransform;
            if (drag) drag.offsetMax = new Vector2(-WindowToolbar.ToolbarHeight * 2, 0);
            RectTransform title = _teamWindow.Toolbar.Find("Title") as RectTransform;
            if (title) title.offsetMax = new Vector2(-WindowToolbar.ToolbarHeight * 2, 0);
            _teamWindow.CaptureEditorReferences();
            _teamWindow.gameObject.SetActive(true);

            Transform fixedApps = _itemParent.Find("FixedApps");
            if (!fixedApps)
            {
                fixedApps = new GameObject("FixedApps", typeof(RectTransform)).transform;
                fixedApps.SetParent(_itemParent, false);
                fixedApps.SetAsFirstSibling();
            }
            HorizontalLayoutGroup layout = fixedApps.GetComponent<HorizontalLayoutGroup>();
            if (!layout) layout = fixedApps.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Transform existing = fixedApps.Find("CurrentTeamButton");
            Button button = existing ? existing.GetComponent<Button>() :
                UiKit.Button(fixedApps, "CurrentTeamButton", "현재 팀", new Color32(255, 174, 76, 255), 18);
            button.transition = Selectable.Transition.None;
            LayoutElement sizing = button.GetComponent<LayoutElement>();
            if (!sizing) sizing = button.gameObject.AddComponent<LayoutElement>();
            sizing.minWidth = sizing.preferredWidth = 168;
            sizing.minHeight = sizing.preferredHeight = 52;
            _teamLauncher = button.GetComponent<InterviewTeamLauncher>();
            if (!_teamLauncher) _teamLauncher = button.gameObject.AddComponent<InterviewTeamLauncher>();
            _teamLauncher.ConfigureEditor(_teamWindow, button, button.GetComponent<Image>());
            LayoutElement appsSize = fixedApps.GetComponent<LayoutElement>();
            if (!appsSize) appsSize = fixedApps.gameObject.AddComponent<LayoutElement>();
            layout.CalculateLayoutInputHorizontal();
            appsSize.minWidth = appsSize.preferredWidth = layout.preferredWidth;
            appsSize.minHeight = appsSize.preferredHeight = 52;
            UnityEditor.EditorUtility.SetDirty(_teamWindow);
            UnityEditor.EditorUtility.SetDirty(_teamLauncher);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
