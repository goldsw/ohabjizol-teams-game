using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [ExecuteAlways, RequireComponent(typeof(GridLayoutGroup))]
    public sealed class ResultResponsiveGrid : MonoBehaviour
    {
        [SerializeField] int columns;
        [SerializeField] int rows;
        [SerializeField] bool square;
        Vector2 lastSize = new Vector2(-1, -1);
        int lastCount = -1;
        void OnEnable() { lastCount = -1; Refresh(); }
        void LateUpdate() { Refresh(); }
        void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) Refresh(); }
        public void Refresh()
        {
            var rect = (RectTransform)transform;
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).gameObject.activeSelf) count++;
            if (rect.rect.size == lastSize && count == lastCount) return;
            lastSize = rect.rect.size; lastCount = count;
            var grid = GetComponent<GridLayoutGroup>();
            int cols = columns > 0 ? columns : Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count *
                Mathf.Max(1, lastSize.x) / Mathf.Max(1, lastSize.y))));
            int rowCount = rows > 0 ? rows : Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));
            float width = Mathf.Max(0, lastSize.x - grid.padding.horizontal - grid.spacing.x * (cols - 1)) / cols;
            float height = Mathf.Max(0, lastSize.y - grid.padding.vertical - grid.spacing.y * (rowCount - 1)) / rowCount;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.cellSize = square ? Vector2.one * Mathf.Min(width, height) : new Vector2(width, height);
        }
#if UNITY_EDITOR
        public void ConfigureEditor(int columnCount, int rowCount, bool squareCells)
        { columns = columnCount; rows = rowCount; square = squareCells; lastCount = -1; Refresh(); }
#endif
    }
}
