using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using Ohabjizol.UI;
#endif

namespace Ohabjizol.Interview
{
    /// <summary>Displays only a user supplied integer score in half-star units.</summary>
    [DisallowMultipleComponent]
    public sealed class TenPointStarsView : MonoBehaviour
    {
        const int StarCount = 5;
        [SerializeField] Text percentageLabel;
        [SerializeField] Text[] emptyStars = new Text[StarCount];
        [SerializeField] Text[] fullStars = new Text[StarCount];
        [SerializeField] RectTransform[] starSlots = new RectTransform[StarCount];
        [SerializeField] RectTransform[] starMasks = new RectTransform[StarCount];
        bool hasScore;
        int score;

        public bool HasAuthoredUi
        {
            get
            {
                if (percentageLabel == null || emptyStars == null || fullStars == null
                    || starSlots == null || starMasks == null || emptyStars.Length != StarCount
                    || fullStars.Length != StarCount || starSlots.Length != StarCount
                    || starMasks.Length != StarCount) return false;
                for (int i = 0; i < StarCount; i++)
                    if (emptyStars[i] == null || fullStars[i] == null
                        || starSlots[i] == null || starMasks[i] == null) return false;
                return true;
            }
        }

        void Awake() { Refresh(); }

        /// <exception cref="ArgumentOutOfRangeException">Score must be an integer from 0 to 10.</exception>
        public void SetScore(int score)
        {
            if (score < 0 || score > 10)
                throw new ArgumentOutOfRangeException(nameof(score), score, "Expected a user score from 0 to 10.");
            this.score = score;
            hasScore = true;
            Refresh();
        }

        public void SetUnavailable()
        {
            hasScore = false;
            score = 0;
            Refresh();
        }

        void Refresh()
        {
            if (!HasAuthoredUi) return;
            percentageLabel.text = hasScore
                ? (score * 10).ToString(CultureInfo.InvariantCulture) + "%" : "—";
            for (int i = 0; i < StarCount; i++)
            {
                int remaining = (hasScore ? score : 0) - i * 2;
                float fill = remaining >= 2 ? 1f : remaining == 1 ? .5f : 0f;
                // The full glyph retains the entire slot width; only its left clip changes.
                starMasks[i].anchorMax = new Vector2(fill, 1f);
                fullStars[i].enabled = fill > 0f;
            }
            ResizeGlyphs();
        }

        void LateUpdate()
        {
            if (HasAuthoredUi) ResizeGlyphs();
        }

        void ResizeGlyphs()
        {
            for (int i = 0; i < StarCount; i++)
                fullStars[i].rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, starSlots[i].rect.width);
        }

#if UNITY_EDITOR
        // These APIs are absent from player builds. All creation is an explicit authoring step.
        public void ConfigureAuthoredUi(Text label, RectTransform[] slots,
            RectTransform[] masks, Text[] empty, Text[] full)
        {
            RequireEditor();
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (slots == null || masks == null || empty == null || full == null
                || slots.Length != StarCount || masks.Length != StarCount
                || empty.Length != StarCount || full.Length != StarCount)
                throw new ArgumentException("Exactly five slots, masks, empty texts and full texts are required.");
            for (int i = 0; i < StarCount; i++)
            {
                if (slots[i] == null || masks[i] == null || empty[i] == null || full[i] == null
                    || masks[i].parent != slots[i] || empty[i].transform.parent != slots[i]
                    || full[i].transform.parent != masks[i] || masks[i].GetComponent<RectMask2D>() == null
                    || masks[i].anchorMin != Vector2.zero || masks[i].pivot.x != 0f
                    || masks[i].offsetMin != Vector2.zero || masks[i].offsetMax != Vector2.zero
                    || full[i].rectTransform.anchorMin != Vector2.zero
                    || full[i].rectTransform.anchorMax != new Vector2(0f, 1f)
                    || full[i].rectTransform.pivot.x != 0f
                    || full[i].rectTransform.anchoredPosition != Vector2.zero)
                    throw new ArgumentException("Each star requires a left-anchored RectMask2D and a fixed-width full glyph.");
            }
            UnityEditor.Undo.RecordObject(this, "Configure ten point stars");
            percentageLabel = label;
            starSlots = (RectTransform[])slots.Clone();
            starMasks = (RectTransform[])masks.Clone();
            emptyStars = (Text[])empty.Clone();
            fullStars = (Text[])full.Clone();
            SetUnavailable();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Captures Star0..Star4, each containing Empty and FillMask/Full.</summary>
        public void CaptureAuthoredUi(RectTransform starsRoot, Text label)
        {
            RequireEditor();
            if (starsRoot == null) throw new ArgumentNullException(nameof(starsRoot));
            var slots = new RectTransform[StarCount];
            var masks = new RectTransform[StarCount];
            var empty = new Text[StarCount];
            var full = new Text[StarCount];
            for (int i = 0; i < StarCount; i++)
            {
                var slot = starsRoot.Find("Star" + i);
                if (slot == null) throw new ArgumentException("Missing Star" + i, nameof(starsRoot));
                slots[i] = slot.GetComponent<RectTransform>();
                var emptyTransform = slot.Find("Empty");
                var mask = slot.Find("FillMask");
                var fullTransform = mask == null ? null : mask.Find("Full");
                empty[i] = emptyTransform == null ? null : emptyTransform.GetComponent<Text>();
                masks[i] = mask == null ? null : mask.GetComponent<RectTransform>();
                full[i] = fullTransform == null ? null : fullTransform.GetComponent<Text>();
            }
            ConfigureAuthoredUi(label, slots, masks, empty, full);
        }

        /// <summary>Builds five slots inside an empty authored stars area; the label is separate.</summary>
        public void BakeAuthoredUi(RectTransform starsRoot, Text label, int fontSize = 40)
        {
            RequireEditor();
            if (starsRoot == null) throw new ArgumentNullException(nameof(starsRoot));
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            if (starsRoot.childCount != 0)
                throw new InvalidOperationException("Use an empty stars root, or CaptureAuthoredUi for existing stars.");
            for (int i = 0; i < StarCount; i++)
            {
                var slotObject = new GameObject("Star" + i, typeof(RectTransform));
                UnityEditor.Undo.RegisterCreatedObjectUndo(slotObject, "Bake ten point stars");
                var slot = (RectTransform)slotObject.transform;
                slot.SetParent(starsRoot, false);
                UiKit.Place(slot, i / (float)StarCount, 0f, (i + 1f) / StarCount, 1f);
                var empty = CreateStar(slot, "Empty", "☆", fontSize);
                empty.color = new Color(.65f, .65f, .65f, 1f);
                var maskObject = new GameObject("FillMask", typeof(RectTransform), typeof(RectMask2D));
                var mask = (RectTransform)maskObject.transform;
                mask.SetParent(slot, false);
                UiKit.Stretch(mask, 0f);
                mask.pivot = new Vector2(0f, .5f);
                var full = CreateStar(mask, "Full", "★", fontSize);
                full.color = new Color(1f, .93f, .22f, 1f);
                full.rectTransform.anchorMax = new Vector2(0f, 1f);
                full.rectTransform.pivot = new Vector2(0f, .5f);
                full.rectTransform.anchoredPosition = Vector2.zero;
            }
            CaptureAuthoredUi(starsRoot, label);
        }

        static Text CreateStar(Transform parent, string name, string glyph, int size)
        {
            var text = UiKit.Text(parent, name, size, FontStyle.Normal, TextAnchor.MiddleCenter);
            UiKit.Stretch(text.rectTransform, 0f);
            text.text = glyph;
            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void RequireEditor()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Star UI authoring is available only outside Play Mode.");
        }
#endif
    }
}