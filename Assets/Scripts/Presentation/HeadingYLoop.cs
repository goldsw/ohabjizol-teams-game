using System.Collections;
using UnityEngine;

namespace Ohabjizol.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class HeadingYLoop : MonoBehaviour
    {
        [SerializeField, Tooltip("Minimum Y offset from this heading's starting position.")] float minY = -10f;
        [SerializeField, Tooltip("Maximum Y offset from this heading's starting position.")] float maxY = 10f;
        [SerializeField, Min(0f), Tooltip("Seconds after the first displayed frame before movement starts.")]
        float startDelay = 3f;
        [SerializeField, Min(0f), Tooltip("Peak speed of the sine target in UI pixels per second.")]
        float moveSpeed = 8f;
        [SerializeField, Range(0f, 1f), Tooltip("Position follow fraction at 60 FPS; adjusted for frame time.")]
        float positionLerp = .12f;
        [SerializeField, Range(0f, 1f), Tooltip("Bounds follow fraction at 60 FPS; adjusted for frame time.")]
        float rangeLerp = .08f;
        RectTransform rect;
        Coroutine motion;
        float originY;
        bool hasOrigin;
        double phase;
        float smoothMin;
        float smoothMax;

        public float MinYOffset => minY;
        public float MaxYOffset => maxY;
        public float MoveSpeed => moveSpeed;

        public void SetMotion(float lowerOffset, float upperOffset, float speed)
        {
            minY = Mathf.Min(lowerOffset, upperOffset);
            maxY = Mathf.Max(lowerOffset, upperOffset);
            moveSpeed = Mathf.Max(0f, speed);
        }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            rect = GetComponent<RectTransform>();
            originY = rect.anchoredPosition.y;
            hasOrigin = true;
            motion = StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            yield return new WaitForEndOfFrame();
            double visibleAt = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - visibleAt < Mathf.Max(0f, startDelay))
                yield return null;

            smoothMin = Mathf.Min(minY, maxY);
            smoothMax = Mathf.Max(minY, maxY);
            float center = (smoothMin + smoothMax) * .5f;
            float radius = (smoothMax - smoothMin) * .5f;
            // The asin branch has a nonnegative slope, so the first target moves toward max.
            phase = radius > .001f ? System.Math.Asin(Mathf.Clamp(-center / radius, -1f, 1f)) : 0d;
            double previous = Time.realtimeSinceStartupAsDouble;
            while (true)
            {
                yield return null;
                double now = Time.realtimeSinceStartupAsDouble;
                float remaining = Mathf.Clamp((float)(now - previous), 0f, .1f);
                previous = now;
                while (remaining > 0f)
                {
                    float step = Mathf.Min(remaining, 1f / 120f);
                    Advance(step);
                    remaining -= step;
                }
            }
        }

        void Advance(float delta)
        {
            float boundsFollow = FrameLerp(rangeLerp, delta);
            smoothMin = Mathf.Lerp(smoothMin, Mathf.Min(minY, maxY), boundsFollow);
            smoothMax = Mathf.Lerp(smoothMax, Mathf.Max(minY, maxY), boundsFollow);
            float center = (smoothMin + smoothMax) * .5f;
            float radius = (smoothMax - smoothMin) * .5f;
            if (radius > .001f)
                phase = (phase + Mathf.Max(0f, moveSpeed) / radius * delta) % (System.Math.PI * 2d);
            float target = originY + center + radius * (float)System.Math.Sin(phase);
            SetY(Mathf.Lerp(rect.anchoredPosition.y, target, FrameLerp(positionLerp, delta)));
        }

        static float FrameLerp(float fraction, float delta)
            => 1f - Mathf.Pow(1f - Mathf.Clamp01(fraction), Mathf.Max(0f, delta) * 60f);

        void SetY(float y)
        {
            Vector2 position = rect.anchoredPosition;
            position.y = y;
            rect.anchoredPosition = position;
        }

        void OnDisable()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            phase = 0d;
            if (rect && hasOrigin) SetY(originY);
            hasOrigin = false;
        }

#if UNITY_EDITOR
        void Reset()
        {
            minY = -10f;
            maxY = 10f;
            startDelay = 3f;
            moveSpeed = 8f;
            positionLerp = .12f;
            rangeLerp = .08f;
        }

        public void ConfigureEditor(float lowerY, float upperY, float delay, float speed)
        {
            SetMotion(lowerY, upperY, speed);
            startDelay = Mathf.Max(0f, delay);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
