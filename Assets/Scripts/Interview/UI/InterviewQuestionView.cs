using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed class InterviewQuestionView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Text _prompt;
        [SerializeField] Image usedOverlay;
        public Button Button => _button;
        public bool HasAuthoredUi => _button && _prompt;
        public void Configure(Button button, Text prompt) { _button = button; _prompt = prompt; }
        public void Bind(string prompt) { _prompt.text = prompt; }

        public void SetUsed(bool used)
        {
            if (!usedOverlay) return;
            if (used)
            {
                CopySourceImage();
                usedOverlay.transform.SetAsLastSibling();
            }
            usedOverlay.gameObject.SetActive(used);
        }

        void CopySourceImage()
        {
            var source = _button ? _button.targetGraphic as Image : null;
            if (!source || !usedOverlay) return;
            usedOverlay.sprite = source.sprite;
            usedOverlay.type = source.type;
            usedOverlay.preserveAspect = source.preserveAspect;
            usedOverlay.fillCenter = source.fillCenter;
            usedOverlay.fillMethod = source.fillMethod;
            usedOverlay.fillOrigin = source.fillOrigin;
            usedOverlay.fillAmount = source.fillAmount;
            usedOverlay.fillClockwise = source.fillClockwise;
            usedOverlay.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            usedOverlay.raycastTarget = false;
        }

#if UNITY_EDITOR
        public void EnsureUsedOverlayEditor()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Question overlays must be authored in Edit Mode.");
            var source = _button ? _button.targetGraphic as Image : null;
            if (!source) throw new System.InvalidOperationException("Question button requires an Image.");
            if (!usedOverlay)
            {
                var existing = source.transform.Find("UsedOverlay");
                if (existing)
                {
                    usedOverlay = existing.GetComponent<Image>();
                    if (!usedOverlay) throw new System.InvalidOperationException("UsedOverlay requires an Image.");
                }
                else
                {
                    var created = new GameObject("UsedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                    created.transform.SetParent(source.transform, false);
                    usedOverlay = created.GetComponent<Image>();
                    usedOverlay.color = new Color(.1f, .1f, .1f, .5f);
                    created.GetComponent<LayoutElement>().ignoreLayout = true;
                }
            }
            var rect = usedOverlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            usedOverlay.transform.SetAsLastSibling();
            CopySourceImage();
            SetUsed(false);
            foreach (var target in new UnityEngine.Object[] { this, usedOverlay, rect, usedOverlay.gameObject })
            {
                UnityEditor.EditorUtility.SetDirty(target);
                if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(target))
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }
#endif
    }
}
