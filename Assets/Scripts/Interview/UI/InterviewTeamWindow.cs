using System;
using Ohabjizol.UI;
using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class InterviewTeamWindow : InfoWindow
    {
        [SerializeField] InterviewTeamSlotView[] _profileSlots;
        [SerializeField] ApplicantVisualCatalog _profileCatalog;

        public void BindTeam(SelectedTeamDisplaySnapshot snapshot)
        {
            if (_profileSlots == null || _profileSlots.Length != 6 || !_profileCatalog)
                throw new InvalidOperationException("Team profiles require an authored catalog and six slots.");
            foreach (var slot in _profileSlots)
                if (!slot || !slot.HasAuthoredPortrait)
                    throw new InvalidOperationException("Team profile slot is incomplete; run BakeTeamProfilesEditor.");
            // Clear every slot before reporting an oversized snapshot, avoiding stale faces.
            for (int i = 0; i < _profileSlots.Length; i++)
                _profileSlots[i].BindPortrait(snapshot != null && i < snapshot.Count ? snapshot.Members[i] : null, _profileCatalog);
            if (snapshot != null && snapshot.Count > _profileSlots.Length)
                throw new InvalidOperationException("Team snapshot exceeds the six authored profile slots.");
        }
#if UNITY_EDITOR
        public void ConfigureProfilesEditor(InterviewTeamSlotView[] slots, ApplicantVisualCatalog catalog)
        {
            if (Application.isPlaying || slots == null || slots.Length != 6 || !catalog)
                throw new InvalidOperationException("ConfigureProfilesEditor requires EditMode, six slots and a catalog.");
            var unique = new System.Collections.Generic.HashSet<InterviewTeamSlotView>();
            foreach (var slot in slots)
                if (!slot || !slot.HasAuthoredPortrait || !unique.Add(slot))
                    throw new InvalidOperationException("Team profiles contain missing, incomplete or duplicate slots.");
            UnityEditor.Undo.RecordObject(this, "Configure team profiles");
            _profileSlots = (InterviewTeamSlotView[])slots.Clone();
            _profileCatalog = catalog;
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        protected override void BuildInfo(RectTransform info) { }
    }
}
