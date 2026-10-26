using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using Ohabjizol.UI;
#endif

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewTeamSlotView
    {
        [SerializeField] ApplicantStatsView _skillStars;

        public bool HasAuthoredSkillStars => _skillStars && _skillStars.HasAuthoredUi;

        void BindSkillStars(SelectedApplicantDisplay member)
        {
            if (!_skillStars) return;
            _skillStars.Bind(member?.DisplayedSkills);
            _skillStars.gameObject.SetActive(member != null);
        }

#if UNITY_EDITOR
        public void BakeSkillStarsEditor(Font font)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Team skill stars require EditMode.");
            if (HasAuthoredSkillStars) return;
            if (!font) throw new ArgumentNullException(nameof(font));
            if (transform.Find("SkillStars"))
                throw new InvalidOperationException("Repair the existing SkillStars references before authoring.");

            var obj = new GameObject("SkillStars", typeof(RectTransform), typeof(LayoutElement), typeof(ApplicantStatsView));
            obj.SetActive(false);
            obj.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(transform, false);
            // An overlay child never participates in the existing row's layout.
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(72f, -70f);
            rect.offsetMax = new Vector2(-8f, -34f);
            _skillStars = obj.GetComponent<ApplicantStatsView>();
            _skillStars.BakeAuthoredUi(font, new Color(.83f, .85f, .88f, 1f), 11);
            rect.Find("Heading").gameObject.SetActive(false);
            string[] rows = { "Planning", "Programming", "Graphics" };
            string[] labels = { "기획", "프로그래밍", "아트" };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = (RectTransform)rect.Find(rows[i]);
                UiKit.Place(row, i / 3f, 0f, (i + 1f) / 3f, 1f);
                var role = row.Find("Role").GetComponent<Text>();
                role.text = labels[i];
                role.alignment = TextAnchor.MiddleCenter;
                UiKit.Place(role.rectTransform, 0f, .56f, 1f, 1f);
                var stars = (RectTransform)row.Find("Stars");
                UiKit.Place(stars, .06f, 0f, .94f, .56f);
                row.Find("Percentage").gameObject.SetActive(false);
                foreach (var star in stars.GetComponentsInChildren<Text>(true))
                    star.fontSize = 16;
            }
            foreach (var graphic in rect.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            _skillStars.SetUnavailable();
            obj.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}
