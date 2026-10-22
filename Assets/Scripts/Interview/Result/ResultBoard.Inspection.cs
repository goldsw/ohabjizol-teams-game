using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class ResultBoard
    {
        [SerializeField] Text[] selectedMemberRows = new Text[6];
        [SerializeField] Text debugText;

        void ShowInspection(InterviewResult value)
        {
            ShowVisualInspection(value);
            if (selectedMemberRows != null)
                for (int i = 0; i < selectedMemberRows.Length; i++)
                {
                    var row = selectedMemberRows[i];
                    if (row == null) continue;
                    row.text = string.Empty;
                    row.color = Color.white;
                    if (value == null || value.SelectedMembers == null || i >= value.SelectedMembers.Count) continue;
                    var member = value.SelectedMembers[i];
                    if (member == null) continue;
                    row.text = member.Name + " / " + RoleLabel(member.Role);
                    row.color = member.HasFalsePortfolio ? Color.red : Color.white;
                }
            if (debugText == null) return;
            debugText.text = value == null ? string.Empty
                : "실제 합계\n" + FormatSkills(value.ActualSkills)
                + "\n요구 합계\n" + FormatSkills(value.RequiredSkills)
                + "\n원본 정확도 " + value.RawAccuracyPercent.ToString("0.####", CultureInfo.InvariantCulture) + "%"
                + "\n표시 정확도 " + value.AccuracyPercent + "%"
                + "\n이번 단계 " + Stars(value.HalfStarUnits) + " / 5별";
        }

        static string RoleLabel(JobRole role)
        {
            switch (role)
            {
                case JobRole.Planning: return "기획";
                case JobRole.Programming: return "프로그래밍";
                case JobRole.Graphics: return "아트";
                default: return role.ToString();
            }
        }

        static string FormatSkills(SkillSet skills)
        {
            return "기획 " + skills.Planning + " / 프로그래밍 " + skills.Programming + " / 아트 " + skills.Graphics;
        }

        public void BakeInspectionUi()
        {
#if UNITY_EDITOR
            BakeAuthoredUi();
            authoredRoot.GetComponent<Canvas>().sortingOrder = 100;
            BakeResultScheduleUi();
            if (selectedMemberRows == null || selectedMemberRows.Length != 6)
                System.Array.Resize(ref selectedMemberRows, 6);
            for (int i = 0; i < selectedMemberRows.Length; i++)
            {
                if (selectedMemberRows[i] != null) continue;
                string name = "SelectedMember" + i;
                var existing = authoredRoot.Find(name);
                selectedMemberRows[i] = existing == null ? null : existing.GetComponent<Text>();
                if (selectedMemberRows[i] == null)
                    selectedMemberRows[i] = ResultAuthoredUi.Label(authoredRoot, name, "", 24,
                        new Vector2(.04f, .77f - i * .047f), new Vector2(.39f, .815f - i * .047f));
                ConfigureInspectionText(selectedMemberRows[i], 24);
            }
            if (debugText == null)
            {
                var existing = authoredRoot.Find("InspectionDebug");
                debugText = existing == null ? null : existing.GetComponent<Text>();
                if (debugText == null)
                    debugText = ResultAuthoredUi.Label(authoredRoot, "InspectionDebug", "", 22,
                        new Vector2(.61f, .49f), new Vector2(.96f, .82f));
                ConfigureInspectionText(debugText, 22);
            }
            PlaceScheduleRect(titleText.rectTransform, .30f, .90f, .70f, .98f);
            PlaceScheduleRect(completionText.rectTransform, .30f, .84f, .70f, .90f);
            completionText.fontSize = 26;
            PlaceScheduleRect(totalStarsText.rectTransform, .30f, .59f, .68f, .69f);
            totalStarsText.fontSize = 24;
            for (int i = 0; i < starSlots.Length; i++)
            {
                float left = .30f + i * .075f;
                PlaceScheduleRect(starSlots[i], left, .71f, left + .075f, .83f);
                emptyStars[i].fontSize = 52;
                fullStars[i].fontSize = 52;
            }
            for (int i = 0; i < selectedMemberRows.Length; i++)
            {
                float top = .84f - i * .037f;
                PlaceScheduleRect(selectedMemberRows[i].rectTransform, .04f, top - .037f, .28f, top);
                ConfigureInspectionText(selectedMemberRows[i], 24);
            }
            PlaceScheduleRect(debugText.rectTransform, .71f, .60f, .98f, .85f);
            ConfigureInspectionText(debugText, 22);
            PlaceScheduleRect(scheduleRoot, .04f, .11f, .96f, .58f);
            PlaceScheduleRect(scheduleSummary.rectTransform, 0f, .79f, 1f, 1f);
            scheduleSummary.fontSize = 22;
            scheduleSummary.lineSpacing = 1f;
            scheduleSummary.resizeTextForBestFit = true;
            scheduleSummary.resizeTextMinSize = 14;
            scheduleSummary.resizeTextMaxSize = 22;
            scheduleSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
            scheduleSummary.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceScheduleRect((RectTransform)scheduleScroll.transform, 0f, 0f, 1f, .78f);
            FitScheduleLayout(6);
            PlaceScheduleRect((RectTransform)continueButton.transform, .35f, .015f, .65f, .095f);
            BakeVisualUi();
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        void ConfigureInspectionText(Text text, int size)
        {
            text.font = titleText.font;
            text.fontSize = size;
            text.lineSpacing = 1f;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
#endif
    }
}
