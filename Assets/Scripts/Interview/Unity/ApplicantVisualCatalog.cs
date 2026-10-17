using System;
using UnityEngine;

namespace Ohabjizol.Interview
{
    [Serializable]
    public sealed class ApplicantVisualDefinition
    {
        [SerializeField] string _characterId;
        [SerializeField] Sprite _idle;
        [SerializeField] Sprite _talk;
        [SerializeField] Sprite _accepted;
        [SerializeField] Sprite _rejected;
        [SerializeField] float _acceptedDuration = 3f;
        [SerializeField] float _rejectedDuration = 3f;

        public string CharacterId => _characterId;
        public Sprite Idle => _idle;
        public Sprite Talk => _talk;
        public Sprite Accepted => _accepted;
        public Sprite Rejected => _rejected;
        public float AcceptedDuration => _acceptedDuration;
        public float RejectedDuration => _rejectedDuration;
    }

    [CreateAssetMenu(menuName = "Interview/Applicant Visual Catalog")]
    public sealed class ApplicantVisualCatalog : ScriptableObject
    {
        [SerializeField] ApplicantVisualDefinition[] _visuals = new ApplicantVisualDefinition[0];

        public bool TryGet(string characterId, out ApplicantVisualDefinition visual)
        {
            visual = null;
            if (string.IsNullOrEmpty(characterId) || _visuals == null) return false;
            foreach (var entry in _visuals)
            {
                if (entry == null || !string.Equals(entry.CharacterId, characterId, StringComparison.Ordinal)) continue;
                // Ambiguous mappings must never select an arbitrary character.
                if (visual != null) { visual = null; return false; }
                visual = entry;
            }
            return visual != null;
        }
    }
}
