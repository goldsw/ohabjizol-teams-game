using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class ResultCalendarCellView : MonoBehaviour
    {
        [SerializeField] Image cellBackgroundImage;
        [SerializeField] Text dayLabel;
        [SerializeField] Image workdayBackground;
        [SerializeField] ResultProfileView[] profiles;
        [SerializeField] ResultResponsiveGrid profileGrid;
        public Image CellBackgroundImage => cellBackgroundImage;

        public void Show(int day, InterviewResult value, ApplicantVisualCatalog catalog)
        {
            bool work = value != null && value.ScheduleResult != null && value.ScheduleResult.ProjectSchedule.IsMarked(day);
            dayLabel.text = day.ToString();
            workdayBackground.gameObject.SetActive(work);
            int slot = 0;
            if (value != null)
                foreach (var member in value.SelectedMembers)
                {
                    if (member == null || !member.UnavailableSchedule.IsMarked(day)) continue;
                    if (slot >= profiles.Length) break;
                    profiles[slot++].Bind(member, catalog, null, true, work);
                }
            for (; slot < profiles.Length; slot++) profiles[slot].gameObject.SetActive(false);
            profileGrid.Refresh();
        }
#if UNITY_EDITOR
        public void ConfigureEditor(Image background, Text day, Image workday,
            ResultProfileView[] pool, ResultResponsiveGrid grid)
        { cellBackgroundImage = background; dayLabel = day; workdayBackground = workday; profiles = pool; profileGrid = grid; }
#endif
    }
}
