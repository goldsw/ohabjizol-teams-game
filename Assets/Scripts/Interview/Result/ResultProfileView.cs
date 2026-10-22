using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [ExecuteAlways]
    public sealed class ResultProfileView : MonoBehaviour
    {
        [SerializeField] Image portrait;
        [SerializeField] Image statusBackground;
        [SerializeField] Text caption;
        [SerializeField] Text fraudLabel;
        [SerializeField] RectTransform profile;
        [SerializeField] RectTransform profileArea;
        [SerializeField] Vector2 referenceSize;
        Vector2 previousSize = new Vector2(-1, -1);
        void OnEnable() { previousSize = new Vector2(-1, -1); LateUpdate(); }
        void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) LateUpdate(); }
        void LateUpdate()
        {
            if (profile == null || profileArea == null || referenceSize.x <= 0 || referenceSize.y <= 0) return;
            var size = profileArea.rect.size;
            if (size == previousSize) return;
            previousSize = size;
            profile.localScale = Vector3.one * Mathf.Max(0, Mathf.Min(size.x / referenceSize.x, size.y / referenceSize.y) * .90f);
        }

        public void Bind(SelectedTeamResultMember member, ApplicantVisualCatalog catalog,
            string label, bool calendar, bool projectDay)
        {
            gameObject.SetActive(member != null);
            if (member == null) return;
            portrait.sprite = catalog != null && catalog.TryGet(member.CharacterId, out var visual) ? visual.Idle : null;
            portrait.enabled = portrait.sprite != null;
            statusBackground.color = calendar
                ? (projectDay ? new Color(.78f, .16f, .20f) : new Color(.13f, .55f, .30f))
                : (member.HasFalsePortfolio ? new Color(.78f, .16f, .20f) : new Color(.16f, .19f, .25f));
            if (caption != null) caption.text = label;
            if (fraudLabel != null) fraudLabel.gameObject.SetActive(member.HasFalsePortfolio);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(Image face, Image background, Text label, Text fraud,
            RectTransform content, RectTransform area, Vector2 sourceSize)
        { portrait = face; statusBackground = background; caption = label; fraudLabel = fraud;
            profile = content; profileArea = area; referenceSize = sourceSize; LateUpdate(); }
#endif
    }
}
