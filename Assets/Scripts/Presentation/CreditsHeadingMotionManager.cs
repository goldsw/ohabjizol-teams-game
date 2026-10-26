using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CreditsHeadingMotionManager : MonoBehaviour
    {
        [SerializeField] Button teamButton;
        [SerializeField] HeadingYLoop[] headings;
        [SerializeField, Min(0f)] float rangeStep = 20f;
        [SerializeField, Min(0f)] float rangeLimit = 100f;
        [SerializeField, Min(0f)] float speedStep = 80f;
        [SerializeField, Min(0f)] float speedLimit = 400f;

        public bool HasAuthoredUi => teamButton && headings != null && headings.Length > 0 &&
            System.Array.TrueForAll(headings, heading => heading != null);

        void OnEnable()
        {
            if (Application.isPlaying && teamButton) teamButton.onClick.AddListener(BoostMotion);
        }

        void OnDisable()
        {
            if (teamButton) teamButton.onClick.RemoveListener(BoostMotion);
        }

        public void BoostMotion()
        {
            if (headings == null) return;
            float cap = Mathf.Max(0f, rangeLimit);
            float step = Mathf.Max(0f, rangeStep);
            foreach (var heading in headings)
            {
                if (!heading) continue;
                float min = Mathf.Clamp(heading.MinYOffset - step, -cap, cap);
                float max = Mathf.Clamp(heading.MaxYOffset + step, -cap, cap);
                float speed = Mathf.Min(Mathf.Max(0f, speedLimit), heading.MoveSpeed + Mathf.Max(0f, speedStep));
                heading.SetMotion(min, max, speed);
            }
        }

#if UNITY_EDITOR
        public void ConfigureEditor(Button team, HeadingYLoop[] targets)
        {
            teamButton = team;
            headings = targets == null ? null : (HeadingYLoop[])targets.Clone();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
