using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>Unreadable ink strokes for decorative document prose.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DocumentScribbleGraphic : MaskableGraphic
    {
        [SerializeField, Range(1, 8)] int lines = 3;
        [SerializeField, Min(.5f)] float thickness = 1.6f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width < 2f || rect.height < 2f) return;
            int rowCount = Mathf.Clamp(lines, 1, 8);
            float rowHeight = rect.height / rowCount;
            float amplitude = Mathf.Min(4f, rowHeight * .16f);
            Color32 ink = color;
            for (int row = 0; row < rowCount; row++)
            {
                float y = rect.yMax - rowHeight * (row + .5f);
                float end = rect.xMin + rect.width * (row % 2 == 0 ? .96f : .81f);
                float cursor = rect.xMin;
                int word = 0;
                while (cursor < end - 10f)
                {
                    float wordWidth = Mathf.Min(27f + ((row * 13 + word * 17) % 31), end - cursor);
                    int steps = Mathf.Max(2, Mathf.CeilToInt(wordWidth / 2.5f));
                    Vector2 previous = StrokePoint(cursor, y, amplitude, row, word, 0f);
                    for (int step = 1; step <= steps; step++)
                    {
                        float t = step * wordWidth / steps;
                        Vector2 next = StrokePoint(cursor + t, y, amplitude, row, word, t);
                        AddStroke(vh, previous, next, thickness, ink);
                        previous = next;
                    }
                    cursor += wordWidth + 9f;
                    word++;
                }
            }
        }

        static Vector2 StrokePoint(float x, float y, float amplitude, int row, int word, float t)
        {
            float phase = row * 1.71f + word * .83f;
            return new Vector2(x, y + amplitude *
                (.65f * Mathf.Sin(t * .85f + phase) + .35f * Mathf.Sin(t * 1.73f + phase)));
        }

        static void AddStroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 ink)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude < .0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, ink, Vector2.zero);
            vh.AddVert(a + normal, ink, Vector2.zero);
            vh.AddVert(b + normal, ink, Vector2.zero);
            vh.AddVert(b - normal, ink, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(int lineCount, float lineThickness)
        {
            lines = Mathf.Clamp(lineCount, 1, 8);
            thickness = Mathf.Max(.5f, lineThickness);
            color = new Color32(45, 45, 45, 210);
            raycastTarget = false;
            SetVerticesDirty();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
