using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ResponsiveGridLayout : MonoBehaviour
{
    public GridLayoutGroup grid;
    public int minColumns = 2;
    public int maxColumns = 6;
    public float minCell = 160f;
    public float padding = 16f;

    void OnEnable(){ if (!grid) grid = GetComponent<GridLayoutGroup>(); UpdateLayout(); }
    void OnRectTransformDimensionsChange(){ UpdateLayout(); }

    void UpdateLayout()
    {
        if (!grid) return;
        var rt = transform as RectTransform;
        float width = rt.rect.width - padding * 2f;
        if (width <= 0) return;


        int cols = Mathf.Clamp(Mathf.FloorToInt(width / (minCell + grid.spacing.x)), minColumns, maxColumns);
        cols = Mathf.Max(1, cols);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;


        float totalSpacing = grid.spacing.x * (cols - 1);
        float cellWidth = (width - totalSpacing) / cols;
        grid.cellSize = new Vector2(cellWidth, cellWidth);
    }
}