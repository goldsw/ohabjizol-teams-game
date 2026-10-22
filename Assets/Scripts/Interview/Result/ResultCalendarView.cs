using System;
using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class ResultCalendarView : MonoBehaviour
    {
        [SerializeField] ResultCalendarCellView[] cells;
        [SerializeField] ApplicantVisualCatalog catalog;

        public bool HasAuthoredUi
        {
            get
            {
                if (catalog == null || cells == null || cells.Length != 30) return false;
                foreach (var cell in cells) if (cell == null) return false;
                return true;
            }
        }

        public void Show(InterviewResult value)
        {
            bool visible = value != null && HasAuthoredUi;
            gameObject.SetActive(visible);
            if (!visible) return;
            for (int i = 0; i < cells.Length; i++) cells[i].Show(i + 1, value, catalog);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(ResultCalendarCellView[] days, ApplicantVisualCatalog visuals)
        {
            if (days == null || days.Length != 30 || visuals == null)
                throw new ArgumentException("Thirty calendar cells and a visual catalog are required.");
            foreach (var day in days)
                if (day == null) throw new ArgumentException("Calendar cell reference is missing.");
            cells = (ResultCalendarCellView[])days.Clone();
            catalog = visuals;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
