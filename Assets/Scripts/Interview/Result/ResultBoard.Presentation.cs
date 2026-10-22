using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class ResultBoard
    {
        [SerializeField] ApplicantVisualCatalog resultVisualCatalog;
        [SerializeField] RectTransform calendarRoot;
        [SerializeField] Image calendarBackgroundImage;
        [SerializeField] ResultCalendarCellView[] calendarCells;
        [SerializeField] ResultProfileView[] rosterProfiles;
        [SerializeField] Image accuracyMaximumYellow;
        [SerializeField] Image accuracyCurrentRed;
        [SerializeField] Text accuracyLabel;
        public Image CalendarBackgroundImage => calendarBackgroundImage;

        void ShowVisualInspection(InterviewResult value)
        {
            if (accuracyCurrentRed != null)
                accuracyCurrentRed.fillAmount = value == null ? 0 : Mathf.Clamp01(value.AccuracyPercent / 100f);
            if (accuracyMaximumYellow != null) accuracyMaximumYellow.fillAmount = 1;
            if (accuracyLabel != null) accuracyLabel.text = value == null ? "정확도 —" : "정확도 " + value.AccuracyPercent + "%";
            if (rosterProfiles == null) return;
            for (int i = 0; i < rosterProfiles.Length; i++)
            {
                var member = value != null && i < value.SelectedMembers.Count ? value.SelectedMembers[i] : null;
                rosterProfiles[i].Bind(member, resultVisualCatalog,
                    member == null ? "" : member.Name + " / " + RoleLabel(member.Role), false, false);
            }
        }

        void ShowCalendar(InterviewResult value)
        {
            calendarRoot.gameObject.SetActive(value != null);
            if (scheduleRoot != null) scheduleRoot.gameObject.SetActive(false);
            var sharedCalendar = calendarRoot.GetComponent<ResultCalendarView>();
            if (sharedCalendar != null) { sharedCalendar.Show(value); return; }
            if (calendarCells == null) return;
            for (int i = 0; i < calendarCells.Length; i++)
                if (calendarCells[i] != null) calendarCells[i].Show(i + 1, value, resultVisualCatalog);
        }
    }
}
