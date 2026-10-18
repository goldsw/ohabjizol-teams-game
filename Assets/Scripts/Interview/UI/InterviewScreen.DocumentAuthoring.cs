using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
#if UNITY_EDITOR
        public void BakeDocumentAppsUi(GameObject libraryPrefab, GameObject buttonPrefab,
            GameObject calendarPrefab, StageScheduleConfig config)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Document migration requires EditMode.");
            if (!_workspace || !_windowParent || !_itemParent)
                throw new InvalidOperationException("Existing document workspace references required.");
            if (!libraryPrefab || !buttonPrefab || !calendarPrefab || !config)
                throw new InvalidOperationException("Document/calendar authored assets required.");
            DocumentLibraryView library = _workspace.Library;
            if (!library)
            {
                Transform existing = _windowParent.Find("DocumentLibrary");
                library = existing ? existing.GetComponent<DocumentLibraryView>() : null;
                if (!library)
                {
                    GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(libraryPrefab, _windowParent);
                    instance.name = "DocumentLibrary";
                    library = instance.GetComponent<DocumentLibraryView>();
                }
            }
            Transform buttonTransform = _itemParent.Find("DocumentLibraryButton");
            if (!buttonTransform) buttonTransform = _itemParent.Find("FixedApps/DocumentLibraryButton");
            GameObject buttonObject = buttonTransform ? buttonTransform.gameObject :
                (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(buttonPrefab, _itemParent);
            buttonObject.name = "DocumentLibraryButton";
            Button libraryButton = buttonObject.GetComponent<Button>();
            buttonObject.GetComponent<DocumentLibraryButton>().ConfigureEditor(_workspace);
            _workspace.ConfigureEditor(library, libraryButton);
            if (!_calendarWindow)
            {
                Transform existing = _windowParent.Find("StageCalendar");
                _calendarWindow = existing ? existing.GetComponent<StageCalendarWindow>() : null;
                if (!_calendarWindow)
                {
                    GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(calendarPrefab, _windowParent);
                    instance.name = "StageCalendar";
                    _calendarWindow = instance.GetComponent<StageCalendarWindow>();
                }
            }
            if (!_calendarButton)
            {
                Transform existing = _itemParent.Find("StageCalendarButton");
                if (!existing) existing = _itemParent.Find("FixedApps/StageCalendarButton");
                _calendarButton = existing ? existing.GetComponent<Button>() : null;
                if (!_calendarButton)
                {
                    GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(buttonPrefab, _itemParent);
                    instance.name = "StageCalendarButton";
                    var documentHandler = instance.GetComponent<DocumentLibraryButton>();
                    if (documentHandler) UnityEngine.Object.DestroyImmediate(documentHandler);
                    _calendarButton = instance.GetComponent<Button>();
                    Text label = instance.GetComponentInChildren<Text>(true);
                    if (label) label.text = "일정표";
                }
            }
            if (!_scheduleConfig) _scheduleConfig = config;
            HorizontalLayoutGroup layout = _itemParent.GetComponent<HorizontalLayoutGroup>();
            if (!layout) layout = _itemParent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            library.gameObject.SetActive(false);
            _calendarWindow.gameObject.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(_workspace);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        public void BakeManagedWindowTaskbarUi(ManagedWindowTaskbarItem itemPrefab)
        {
            if (Application.isPlaying || !_workspace || !_itemParent || !_windowParent || !itemPrefab)
                throw new InvalidOperationException("Authored taskbar references required in EditMode.");
            RectTransform bar = _itemParent;
            if (bar.rect.width < 900f) bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 900f);
            HorizontalLayoutGroup fixedLayout = bar.GetComponent<HorizontalLayoutGroup>();
            if (!fixedLayout) fixedLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            fixedLayout.spacing = 8f;
            fixedLayout.childAlignment = TextAnchor.MiddleLeft;
            fixedLayout.childControlWidth = fixedLayout.childControlHeight = true;
            fixedLayout.childForceExpandWidth = fixedLayout.childForceExpandHeight = false;
            Transform fixedExisting = bar.Find("FixedApps");
            GameObject fixedHost = fixedExisting ? fixedExisting.gameObject : new GameObject("FixedApps", typeof(RectTransform));
            fixedHost.transform.SetParent(bar, false);
            fixedHost.transform.SetAsFirstSibling();
            HorizontalLayoutGroup appsLayout = fixedHost.GetComponent<HorizontalLayoutGroup>();
            if (!appsLayout) appsLayout = fixedHost.AddComponent<HorizontalLayoutGroup>();
            appsLayout.spacing = 8f; appsLayout.childAlignment = TextAnchor.MiddleLeft;
            appsLayout.childControlWidth = appsLayout.childControlHeight = true;
            appsLayout.childForceExpandWidth = appsLayout.childForceExpandHeight = false;
            LayoutElement appsSize = fixedHost.GetComponent<LayoutElement>();
            if (!appsSize) appsSize = fixedHost.AddComponent<LayoutElement>();
            appsSize.minWidth = appsSize.preferredWidth = 344f;
            appsSize.minHeight = appsSize.preferredHeight = 52f;
            Transform libraryLauncher = bar.Find("DocumentLibraryButton");
            if (libraryLauncher) libraryLauncher.SetParent(fixedHost.transform, false);
            if (_calendarButton) _calendarButton.transform.SetParent(fixedHost.transform, false);
            Transform previous = bar.Find("ManagedWindows");
            GameObject host = previous ? previous.gameObject : new GameObject("ManagedWindows", typeof(RectTransform));
            RectTransform viewport = (RectTransform)host.transform;
            viewport.SetParent(bar, false);
            viewport.SetAsLastSibling();
            LayoutElement size = host.GetComponent<LayoutElement>();
            if (!size) size = host.AddComponent<LayoutElement>();
            size.minWidth = 200f; size.preferredWidth = 548f; size.flexibleWidth = 1f;
            size.minHeight = size.preferredHeight = 52f;
            if (!host.GetComponent<RectMask2D>()) host.AddComponent<RectMask2D>();
            Image hit = host.GetComponent<Image>();
            if (!hit) hit = host.AddComponent<Image>();
            hit.color = Color.clear; hit.raycastTarget = true;
            ScrollRect scroll = host.GetComponent<ScrollRect>();
            if (!scroll) scroll = host.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            Transform existing = viewport.Find("Items");
            GameObject contentObject = existing ? existing.gameObject : new GameObject("Items", typeof(RectTransform));
            RectTransform content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            content.anchorMin = Vector2.zero; content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, .5f); content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup dynamicLayout = content.GetComponent<HorizontalLayoutGroup>();
            if (!dynamicLayout) dynamicLayout = contentObject.AddComponent<HorizontalLayoutGroup>();
            dynamicLayout.spacing = 8f; dynamicLayout.childAlignment = TextAnchor.MiddleLeft;
            dynamicLayout.childControlWidth = dynamicLayout.childControlHeight = true;
            dynamicLayout.childForceExpandWidth = dynamicLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (!fitter) fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            WindowTaskbarView view = host.GetComponent<WindowTaskbarView>();
            if (!view) view = host.AddComponent<WindowTaskbarView>();
            view.ConfigureEditor(content, itemPrefab, _windowParent);
            _workspace.ConfigureTaskbarEditor(view);
            if (_teamWindow) BakeTeamWindow();
            UnityEditor.EditorUtility.SetDirty(_workspace);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
