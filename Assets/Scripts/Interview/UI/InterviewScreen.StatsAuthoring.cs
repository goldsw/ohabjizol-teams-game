using System;
using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
#if UNITY_EDITOR
        /// <summary>Adds only the stats panel in the free area below the existing team summary.</summary>
        public void BakeApplicantStatsUi(Font font)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stats authoring requires EditMode.");
            if (!_discordViewRoot || !_currentLabel || !_requiredLabel || !font)
                throw new InvalidOperationException("Existing summary references and a persistent font are required.");
            if (_applicantStats)
            {
                if (!_applicantStats.HasAuthoredUi) throw new InvalidOperationException("Stats references are incomplete.");
                return;
            }
            Transform existing = _discordViewRoot.Find("ApplicantStats");
            if (existing)
            {
                _applicantStats = existing.GetComponent<ApplicantStatsView>();
                if (!_applicantStats || !_applicantStats.HasAuthoredUi)
                    throw new InvalidOperationException("Existing ApplicantStats requires authored references.");
            }
            else
            {
                // Inspect the authored summary instead of moving any existing labels or windows.
                var summary = _currentLabel.transform.parent as RectTransform;
                if (!summary || summary.parent != _discordViewRoot || _requiredLabel.transform.parent != summary
                    || summary.anchorMin.y < .13f || summary.anchorMin.x >= summary.anchorMax.x)
                    throw new InvalidOperationException("No verified free area below the authored team summary.");
                RectTransform host = Rect(_discordViewRoot, "ApplicantStats",
                    summary.anchorMin.x, .005f, summary.anchorMax.x, summary.anchorMin.y - .01f);
                _applicantStats = host.gameObject.AddComponent<ApplicantStatsView>();
                _applicantStats.BakeAuthoredUi(font, _currentLabel.color);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}