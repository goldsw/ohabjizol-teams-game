#if UNITY_EDITOR
using System;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class ResultBoard
    {
        const string ProfileTemplatePath = "Assets/Prefabs/InterviewRefresh/ResultProfileTemplate.prefab";

        public void BakeVisualUi()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Bake result UI outside Play mode.");
            if (authoredRoot == null) BakeAuthoredUi();
            resultVisualCatalog = AssetDatabase.LoadAssetAtPath<ApplicantVisualCatalog>("Assets/Art/Applicants/ApplicantVisualCatalog.asset");
            if (resultVisualCatalog == null) throw new InvalidOperationException("Applicant visual catalog missing.");
            var template = EnsureResultProfileTemplate();
            if (calendarRoot == null)
            {
                calendarRoot = VisualRect(authoredRoot, "ResultCalendar", .035f, .075f, .68f, .77f);
                calendarBackgroundImage = VisualImage(calendarRoot, new Color(.07f, .09f, .13f, .96f));
                var header = VisualLabel(calendarRoot, "Legend", "30일 개발 일정   ■ 작업일    빨강: 작업일 휴무 / 초록: 비작업일 휴무", 22, 0, .92f, 1, 1);
                header.color = Color.white;
                var calendarGrid = VisualRect(calendarRoot, "CalendarGrid", .012f, .015f, .988f, .91f);
                VisualGrid(calendarGrid, 7, 5, false);
                calendarCells = new ResultCalendarCellView[30];
                for (int i = 0; i < 35; i++)
                {
                    var cell = VisualRect(calendarGrid, "Day" + (i + 1).ToString("00"), 0, 0, 1, 1);
                    var background = VisualImage(cell, new Color(.18f, .21f, .27f));
                    if (i >= 30) { background.color = new Color(.11f, .13f, .17f); continue; }
                    var day = VisualLabel(cell, "DayNumber", (i + 1).ToString(), 22, .04f, .74f, .28f, 1);
                    var workday = VisualRect(cell, "Workday", .32f, .77f, .98f, .98f);
                    var workImage = VisualImage(workday, new Color(.08f, .35f, .78f));
                    VisualLabel(workday, "Label", "작업일", 16, 0, 0, 1, 1);
                    workday.gameObject.SetActive(false);
                    var poolRoot = VisualRect(cell, "OffdayProfiles", .025f, .025f, .975f, .72f);
                    poolRoot.gameObject.AddComponent<RectMask2D>();
                    var grid = VisualGrid(poolRoot, 0, 0, true);
                    var pool = new ResultProfileView[6];
                    for (int p = 0; p < pool.Length; p++)
                    {
                        pool[p] = MakeProfile(poolRoot, template, "Offday" + p, false);
                        pool[p].gameObject.SetActive(false);
                    }
                    calendarCells[i] = cell.gameObject.AddComponent<ResultCalendarCellView>();
                    calendarCells[i].ConfigureEditor(background, day, workImage, pool, grid);
                }
            }
            if (rosterProfiles == null || rosterProfiles.Length != 6 || rosterProfiles[0] == null)
            {
                var roster = VisualRect(authoredRoot, "SelectedRoster", .705f, .49f, .975f, .94f);
                VisualImage(roster, new Color(.07f, .09f, .13f, .96f));
                VisualLabel(roster, "Title", "최종 합격자", 30, .03f, .88f, .97f, 1);
                var entries = VisualRect(roster, "Entries", .025f, .02f, .975f, .87f);
                VisualGrid(entries, 1, 6, false);
                rosterProfiles = new ResultProfileView[6];
                for (int i = 0; i < 6; i++) rosterProfiles[i] = MakeProfile(entries, template, "Member" + i, true);
            }
            if (accuracyCurrentRed == null)
            {
                var bar = VisualRect(authoredRoot, "Accuracy", .055f, .795f, .66f, .855f);
                accuracyMaximumYellow = VisualImage(bar, new Color(1f, .83f, .16f));
                var current = VisualRect(bar, "CurrentRed", 0, 0, 1, 1);
                accuracyCurrentRed = VisualImage(current, new Color(.86f, .19f, .22f));
                // Filled Images require a Sprite; the built-in UI sprite keeps the authored fill functional.
                var fillSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                accuracyMaximumYellow.sprite = accuracyCurrentRed.sprite = fillSprite;
                foreach (var image in new[] { accuracyMaximumYellow, accuracyCurrentRed })
                { image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; }
                accuracyMaximumYellow.fillAmount = 1;
                accuracyCurrentRed.fillAmount = 0;
                accuracyLabel = VisualLabel(bar, "AccuracyPercent", "정확도 —", 26, 0, 0, 1, 1);
            }
            var calendarView = calendarRoot.GetComponent<ResultCalendarView>();
            if (calendarView == null) calendarView = calendarRoot.gameObject.AddComponent<ResultCalendarView>();
            calendarView.ConfigureEditor(calendarCells, resultVisualCatalog);
            foreach (var row in selectedMemberRows) if (row != null) row.gameObject.SetActive(false);
            if (scheduleRoot != null) scheduleRoot.gameObject.SetActive(false);
            PlaceScheduleRect(titleText.rectTransform, .04f, .90f, .67f, .98f);
            titleText.fontSize = 42;
            PlaceScheduleRect(completionText.rectTransform, .04f, .855f, .67f, .90f);
            PlaceScheduleRect(totalStarsText.rectTransform, .71f, .34f, .97f, .42f);
            totalStarsText.fontSize = 23;
            for (int i = 0; i < starSlots.Length; i++)
            { PlaceScheduleRect(starSlots[i], .71f + i * .052f, .425f, .76f + i * .052f, .485f); emptyStars[i].fontSize = fullStars[i].fontSize = 46; }
            PlaceScheduleRect(debugText.rectTransform, .71f, .125f, .975f, .33f);
            debugText.fontSize = 18;
            debugText.resizeTextForBestFit = true; debugText.resizeTextMinSize = 10; debugText.resizeTextMaxSize = 18;
            PlaceScheduleRect((RectTransform)continueButton.transform, .75f, .025f, .975f, .105f);
            foreach (var text in authoredRoot.GetComponentsInChildren<Text>(true)) text.font = UiKit.Font;
            EditorUtility.SetDirty(this);
        }

        static GameObject EnsureResultProfileTemplate()
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(ProfileTemplatePath);
            if (template != null) return template;
            const string sourcePath = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";
            var source = PrefabUtility.LoadPrefabContents(sourcePath);
            GameObject copy = null;
            try
            {
                var profile = source.transform.Find("InterviewAuthoredUi/DiscordViewRoot/CallChannelAnchor/CallParticipants/SlotGroup0/Profile");
                if (profile == null) throw new InvalidOperationException("Exact authored source Profile missing: " + sourcePath);
                copy = UnityEngine.Object.Instantiate(profile.gameObject);
                copy.name = "Profile";
                template = PrefabUtility.SaveAsPrefabAsset(copy, ProfileTemplatePath);
                if (template == null) throw new InvalidOperationException("Could not export Profile template.");
            }
            finally
            {
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
                PrefabUtility.UnloadPrefabContents(source);
            }
            return template;
        }

        static ResultProfileView MakeProfile(Transform parent, GameObject template, string name, bool roster)
        {
            var wrapper = VisualRect(parent, name, 0, 0, 1, 1);
            var state = VisualImage(wrapper, new Color(.16f, .19f, .25f));
            var profileArea = VisualRect(wrapper, "ProfileArea", 0, 0, roster ? .20f : 1, 1);
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(template, profileArea);
            var rect = (RectTransform)copy.transform;
            var referenceSize = rect.rect.size;
            if (referenceSize.x <= 0 || referenceSize.y <= 0) referenceSize = new Vector2(56, 56);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = referenceSize; rect.anchoredPosition = Vector2.zero;
            var layout = copy.GetComponent<LayoutElement>();
            if (layout != null) layout.ignoreLayout = false;
            var portrait = copy.transform.Find("ProfileMask/Portrait").GetComponent<Image>();
            Text label = null, fraud = null;
            if (roster)
            {
                label = VisualLabel(wrapper, "NameAndRole", "", 23, .22f, .29f, .98f, .95f);
                label.alignment = TextAnchor.MiddleLeft;
                label.resizeTextForBestFit = true; label.resizeTextMinSize = 13; label.resizeTextMaxSize = 23;
                fraud = VisualLabel(wrapper, "FalsePortfolio", "허위 포트폴리오", 16, .22f, .02f, .98f, .32f);
                fraud.alignment = TextAnchor.MiddleLeft; fraud.color = new Color(1, .65f, .65f);
            }
            var view = wrapper.gameObject.AddComponent<ResultProfileView>();
            view.ConfigureEditor(portrait, state, label, fraud, rect, profileArea, referenceSize);
            return view;
        }

        static RectTransform VisualRect(Transform parent, string name, float x0, float y0, float x1, float y1)
            => ResultAuthoredUi.Rect(parent, name, new Vector2(x0, y0), new Vector2(x1, y1));
        static Image VisualImage(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        static Text VisualLabel(Transform parent, string name, string caption, int size, float x0, float y0, float x1, float y1)
        {
            var text = ResultAuthoredUi.Label(parent, name, caption, size, new Vector2(x0, y0), new Vector2(x1, y1));
            text.color = Color.white; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        static ResultResponsiveGrid VisualGrid(RectTransform rect, int columns, int rows, bool square)
        {
            var layout = rect.gameObject.AddComponent<GridLayoutGroup>();
            layout.spacing = new Vector2(4, 4); layout.childAlignment = TextAnchor.MiddleCenter;
            var grid = rect.gameObject.AddComponent<ResultResponsiveGrid>();
            grid.ConfigureEditor(columns, rows, square); return grid;
        }
    }
}
#endif
