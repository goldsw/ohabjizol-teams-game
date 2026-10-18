using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class ResultBoard
    {
        [SerializeField] RectTransform scheduleRoot;
        [SerializeField] RectTransform scheduleContent;
        [SerializeField] ScrollRect scheduleScroll;
        [SerializeField] Text scheduleSummary;
        [SerializeField] Text scheduleNames;
        [SerializeField] Text[] scheduleDays = new Text[30];

        Coroutine scheduleFitRoutine;

        void ShowSchedule(InterviewResult value)
        {
            if (scheduleFitRoutine != null)
            {
                StopCoroutine(scheduleFitRoutine);
                scheduleFitRoutine = null;
            }
            if (scheduleRoot == null) return;
            scheduleRoot.gameObject.SetActive(value != null);
            if (value == null || scheduleSummary == null) return;
            var schedule = value.ScheduleResult;
            if (schedule == null)
            {
                scheduleSummary.text = "일정 정보 없음";
                if (scheduleScroll != null) scheduleScroll.gameObject.SetActive(false);
                return;
            }
            if (scheduleScroll == null || scheduleContent == null || scheduleNames == null
                || scheduleDays == null || scheduleDays.Length != 30) return;
            for (int day = 0; day < 30; day++) if (scheduleDays[day] == null) return;
            scheduleScroll.gameObject.SetActive(true);
            var participants = schedule.Participants;
            int count = participants == null ? 0 : participants.Count;
            scheduleSummary.text = "최종 합격자 " + count + "명 · 충돌일: "
                + (schedule.ConflictingDays == null || schedule.ConflictingDays.Count == 0
                    ? "없음" : string.Join(", ", schedule.ConflictingDays) + "일")
                + "\n파랑 ■ 제작일 / 노랑 × 불가일 / 빨강 ! 충돌 · 드래그하여 일정 확인";
            var names = new StringBuilder("이름 / 날짜\n제작 일정");
            for (int i = 0; i < count; i++)
                names.Append('\n').Append(CleanScheduleName(participants[i].ApplicantName));
            scheduleNames.text = names.ToString();
            for (int day = 1; day <= 30; day++)
            {
                bool projectDay = schedule.ProjectSchedule.IsMarked(day);
                var column = new StringBuilder(day.ToString("00"));
                column.Append('\n').Append(projectDay ? "<color=#72C9FF>■</color>" : "·");
                for (int i = 0; i < count; i++)
                {
                    var participant = participants[i];
                    bool unavailable = participant.UnavailableSchedule.IsMarked(day);
                    bool conflict = false;
                    if (participant.ConflictDays != null)
                        foreach (int conflictDay in participant.ConflictDays)
                            if (conflictDay == day) { conflict = true; break; }
                    column.Append('\n').Append(conflict ? "<color=#FF7777>!</color>"
                        : unavailable ? "<color=#FFD879>×</color>" : "·");
                }
                scheduleDays[day - 1].text = column.ToString();
            }
            Canvas.ForceUpdateCanvases();
            FitScheduleLayout(count);
            if (Application.isPlaying && isActiveAndEnabled)
                scheduleFitRoutine = StartCoroutine(RefitScheduleWhenSized(count));
        }

        // Shared by the runtime renderer and the editor presentation bake.
        public void FitScheduleLayout(int participantCount)
        {
            if (scheduleRoot == null || scheduleScroll == null || scheduleContent == null
                || scheduleNames == null || scheduleDays == null) return;

            scheduleScroll.horizontal = true;
            scheduleScroll.vertical = false;
            scheduleScroll.velocity = Vector2.zero;
            var viewport = scheduleScroll.viewport;
            if (viewport == null) return;
            scheduleRoot.ForceUpdateRectTransforms();
            viewport.ForceUpdateRectTransforms();
            float height = viewport.rect.height;
            // Keep authored sizes when prefab/canvas geometry has not resolved yet.
            if (viewport.rect.width <= 0f || height <= 16f || scheduleNames.font == null) return;

            int rows = Mathf.Max(0, participantCount) + 2;
            var probe = new StringBuilder();
            for (int row = 0; row < rows; row++)
            {
                if (row != 0) probe.Append('\n');
                probe.Append("가Ag■×!…");
            }
            string rowProbe = probe.ToString();
            float available = height - 16f;
            int fontSize = 22;
            // Actual Noto font metrics determine the largest size that fits all rows.
            for (; fontSize > 1; fontSize--)
                if (MeasureScheduleRows(rowProbe, fontSize) <= available) break;

            scheduleContent.anchorMin = new Vector2(0f, 1f);
            scheduleContent.anchorMax = new Vector2(0f, 1f);
            scheduleContent.pivot = new Vector2(0f, 1f);
            scheduleContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            ApplyScheduleColumn(scheduleNames, fontSize);
            foreach (var day in scheduleDays)
                if (day != null) ApplyScheduleColumn(day, fontSize);
            var position = scheduleContent.anchoredPosition;
            position.y = 0f;
            scheduleContent.anchoredPosition = position;
            scheduleScroll.verticalNormalizedPosition = 1f;
            scheduleScroll.horizontalNormalizedPosition = 0f;
        }

        float MeasureScheduleRows(string probe, int fontSize)
        {
            var settings = scheduleNames.GetGenerationSettings(Vector2.zero);
            settings.font = scheduleNames.font;
            settings.fontSize = fontSize;
            settings.fontStyle = FontStyle.Normal;
            settings.lineSpacing = 1f;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var generator = scheduleNames.cachedTextGeneratorForLayout;
            float height = generator.GetPreferredHeight(probe, settings);
            height = Mathf.Max(height, generator.GetPreferredHeight(scheduleNames.text, settings));
            foreach (var day in scheduleDays)
                if (day != null)
                    height = Mathf.Max(height, generator.GetPreferredHeight(day.text, settings));
            return height / scheduleNames.pixelsPerUnit;
        }

        void ApplyScheduleColumn(Text text, int fontSize)
        {
            text.font = scheduleNames.font;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = false;
            text.lineSpacing = 1f;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = text == scheduleNames ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            var rect = text.rectTransform;
            var min = rect.offsetMin;
            var max = rect.offsetMax;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(min.x, 8f);
            rect.offsetMax = new Vector2(max.x, -8f);
        }

        System.Collections.IEnumerator RefitScheduleWhenSized(int participantCount)
        {
            var previousSize = new Vector2(-1f, -1f);
            while (scheduleRoot != null && scheduleRoot.gameObject.activeInHierarchy)
            {
                yield return null;
                if (scheduleScroll == null || scheduleScroll.viewport == null) continue;
                var size = scheduleScroll.viewport.rect.size;
                if (size == previousSize) continue;
                FitScheduleLayout(participantCount);
                previousSize = scheduleScroll.viewport.rect.size;
            }
            scheduleFitRoutine = null;
        }

        static string CleanScheduleName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "이름 없음";
            string clean = value.Replace('\r', ' ').Replace('\n', ' ');
            return clean.Length > 10 ? clean.Substring(0, 9) + "…" : clean;
        }

        public void BakeResultScheduleUi()
        {
#if UNITY_EDITOR
            if (authoredRoot == null) BakeAuthoredUi();
            if (!HasAuthoredUi)
                throw new System.InvalidOperationException("Result schedule bake requires valid ResultBoard authored UI references (title, completion, stars, totals and continue button).");
            if (scheduleRoot != null)
            {
                FitScheduleLayout(6);
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }
            // Reserve the lower half for the scrollable schedule; keep existing score and actions.
            PlaceScheduleRect(titleText.rectTransform, .1f, .88f, .9f, .98f);
            PlaceScheduleRect(completionText.rectTransform, .1f, .81f, .9f, .88f);
            PlaceScheduleRect(totalStarsText.rectTransform, .1f, .59f, .9f, .70f);
            totalStarsText.fontSize = 28;
            for (int i = 0; i < starSlots.Length; i++)
            {
                PlaceScheduleRect(starSlots[i], .25f + i * .1f, .70f, .35f + i * .1f, .81f);
                emptyStars[i].fontSize = 64;
                fullStars[i].fontSize = 64;
            }
            PlaceScheduleRect((RectTransform)continueButton.transform, .35f, .015f, .65f, .095f);
            scheduleRoot = ScheduleRect(authoredRoot, "ResultSchedule", .04f, .11f, .96f, .58f);
            scheduleSummary = ScheduleText(scheduleRoot, "Summary", false);
            PlaceScheduleRect(scheduleSummary.rectTransform, 0f, .79f, 1f, 1f);
            scheduleSummary.fontSize = 22;
            var scrollRoot = ScheduleRect(scheduleRoot, "ScheduleScroll", 0f, 0f, 1f, .78f);
            var surface = scrollRoot.gameObject.AddComponent<Image>();
            surface.color = new Color(.06f, .08f, .12f, .96f);
            surface.raycastTarget = true;
            scheduleScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = ScheduleRect(scrollRoot, "Viewport", 0f, 0f, 1f, 1f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scheduleContent = ScheduleRect(viewport, "Content", 0f, 1f, 0f, 1f);
            scheduleContent.pivot = new Vector2(0f, 1f);
            scheduleContent.sizeDelta = new Vector2(1460f, 100f);
            scheduleScroll.viewport = viewport;
            scheduleScroll.content = scheduleContent;
            scheduleScroll.horizontal = true;
            scheduleScroll.vertical = false;
            scheduleScroll.movementType = ScrollRect.MovementType.Clamped;
            scheduleScroll.scrollSensitivity = 32f;
            scheduleNames = ScheduleText(scheduleContent, "Names", false);
            ScheduleColumn(scheduleNames.rectTransform, 12f, 240f);
            scheduleDays = new Text[30];
            for (int day = 0; day < 30; day++)
            {
                scheduleDays[day] = ScheduleText(scheduleContent, "Day" + (day + 1).ToString("00"), true);
                scheduleDays[day].alignment = TextAnchor.UpperCenter;
                ScheduleColumn(scheduleDays[day].rectTransform, 252f + day * 40f, 40f);
            }
            scheduleSummary.text = "일정 정보 없음";
            FitScheduleLayout(6);
            scheduleScroll.gameObject.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        static RectTransform ScheduleRect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            PlaceScheduleRect(rect, x0, y0, x1, y1);
            return rect;
        }

        static void PlaceScheduleRect(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Text ScheduleText(Transform parent, string name, bool richText)
        {
            var text = Ohabjizol.UI.UiKit.Text(parent, name, 22, FontStyle.Normal, TextAnchor.UpperLeft);
            text.supportRichText = richText;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.4f;
            return text;
        }

        static void ScheduleColumn(RectTransform rect, float left, float width)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(left + width, -8f);
        }
#endif
    }
}