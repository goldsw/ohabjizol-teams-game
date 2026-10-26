using System.Collections;
using UnityEngine;

namespace Ohabjizol.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIScaleLoop : MonoBehaviour
    {
        [SerializeField, Min(0f)] float minScale = .9f;
        [SerializeField, Min(0f)] float maxScale = 1.2f;
        [SerializeField, Min(.05f)] float cycleDuration = 4f;
        RectTransform rect;
        Vector3 originScale;
        bool hasOrigin;
        Coroutine pulse;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            rect = GetComponent<RectTransform>();
            originScale = rect.localScale;
            hasOrigin = true;
            pulse = StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            yield return new WaitForEndOfFrame();
            float lower = Mathf.Max(0f, Mathf.Min(minScale, maxScale));
            float upper = Mathf.Max(lower, Mathf.Max(minScale, maxScale));
            float center = (lower + upper) * .5f;
            float radius = (upper - lower) * .5f;
            double phase = radius > .0001f
                ? System.Math.Asin(Mathf.Clamp((1f - center) / radius, -1f, 1f)) : 0d;
            double previous = Time.realtimeSinceStartupAsDouble;
            while (true)
            {
                double now = Time.realtimeSinceStartupAsDouble;
                phase = (phase + System.Math.Max(0d, now - previous) * System.Math.PI * 2d /
                    Mathf.Max(.05f, cycleDuration)) % (System.Math.PI * 2d);
                previous = now;
                lower = Mathf.Max(0f, Mathf.Min(minScale, maxScale));
                upper = Mathf.Max(lower, Mathf.Max(minScale, maxScale));
                float multiplier = (lower + upper) * .5f +
                    (upper - lower) * .5f * (float)System.Math.Sin(phase);
                rect.localScale = originScale * multiplier;
                yield return null;
            }
        }

        void OnDisable()
        {
            if (pulse != null) StopCoroutine(pulse);
            pulse = null;
            if (rect && hasOrigin) rect.localScale = originScale;
            hasOrigin = false;
        }

#if UNITY_EDITOR
        public void ConfigureEditor(float minimum, float maximum, float duration)
        {
            minScale = Mathf.Max(0f, Mathf.Min(minimum, maximum));
            maxScale = Mathf.Max(minScale, Mathf.Max(minimum, maximum));
            cycleDuration = Mathf.Max(.05f, duration);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
