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
#if UNITY_EDITOR
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
            team.SetParent(_teamWindow.Info, false);
            team.anchorMin = Vector2.zero; team.anchorMax = Vector2.one;
            team.offsetMin = team.offsetMax = Vector2.zero;
            team.localScale = Vector3.one;
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
