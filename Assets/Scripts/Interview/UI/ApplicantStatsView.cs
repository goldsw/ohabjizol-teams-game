using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using Ohabjizol.UI;
#endif

namespace Ohabjizol.Interview
{
    /// <summary>Receives only the effective displayed skills, never applicant truth data.</summary>
    [DisallowMultipleComponent]
    public sealed class ApplicantStatsView : MonoBehaviour
    {
        [SerializeField] TenPointStarsView planning;
        [SerializeField] TenPointStarsView programming;
        [SerializeField] TenPointStarsView graphics;
        [SerializeField] Text heading;
        [SerializeField] Text[] roleLabels = new Text[3];

        public bool HasAuthoredUi => planning && planning.HasAuthoredUi
            && programming && programming.HasAuthoredUi && graphics && graphics.HasAuthoredUi
            && heading && roleLabels != null && roleLabels.Length == 3
            && roleLabels[0] && roleLabels[1] && roleLabels[2];

        public void Bind(SkillSet displayed)
        {
            if (displayed == null) { SetUnavailable(); return; }
            BindRow(planning, displayed.Planning);
            BindRow(programming, displayed.Programming);
            BindRow(graphics, displayed.Graphics);
        }

        public void SetUnavailable()
        {
            if (planning) planning.SetUnavailable();
            if (programming) programming.SetUnavailable();
            if (graphics) graphics.SetUnavailable();
        }

        static void BindRow(TenPointStarsView view, int value)
        {
            if (!view) return;
            // Legacy stages may contain 11..40: do not disguise them as ten-point scores.
            if (value < 0 || value > 10) view.SetUnavailable();
            else view.SetScore(value);
        }

#if UNITY_EDITOR
        public void BakeAuthoredUi(Font font, Color textColor, int fontSize = 18)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stats authoring requires EditMode.");
            if (HasAuthoredUi) return;
            if (!font) throw new ArgumentNullException(nameof(font));
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            if (!(transform is RectTransform) || transform.childCount != 0)
                throw new InvalidOperationException("An empty RectTransform is required for stats authoring.");
            heading = Label(transform, "Heading", "예상능력", font, textColor, fontSize,
                0f, .77f, 1f, 1f, TextAnchor.MiddleLeft);
            string[] names = { "기획", "프로그래밍", "그래픽" };
            string[] keys = { "Planning", "Programming", "Graphics" };
            var rows = new TenPointStarsView[3];
            roleLabels = new Text[3];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = Rect(transform, keys[i], 0f, .50f - i * .25f, 1f, .75f - i * .25f);
                roleLabels[i] = Label(row, "Role", names[i], font, textColor, fontSize,
                    0f, 0f, .27f, 1f, TextAnchor.MiddleLeft);
                var starsRoot = Rect(row, "Stars", .28f, 0f, .82f, 1f);
                Text percentage = Label(row, "Percentage", "—", font, textColor, fontSize,
                    .83f, 0f, 1f, 1f, TextAnchor.MiddleRight);
                rows[i] = row.gameObject.AddComponent<TenPointStarsView>();
                rows[i].BakeAuthoredUi(starsRoot, percentage, fontSize + 10);
                foreach (Text text in starsRoot.GetComponentsInChildren<Text>(true)) text.font = font;
            }
            planning = rows[0]; programming = rows[1]; graphics = rows[2];
            SetUnavailable();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        static RectTransform Rect(Transform parent, string name, float x, float y, float right, float top)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            UiKit.Place(rect, x, y, right, top);
            return rect;
        }

        static Text Label(Transform parent, string name, string caption, Font font, Color color,
            int size, float x, float y, float right, float top, TextAnchor alignment)
        {
            var label = UiKit.Text(parent, name, size, FontStyle.Normal, alignment);
            label.font = font;
            label.color = color;
            label.text = caption;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            UiKit.Place(label.rectTransform, x, y, right, top);
            return label;
        }
#endif
    }
}