using UnityEngine;

public class PlatformGridGenerator : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridColumns;
    public int gridRows;

    [Header("Visualize Grid")]
    [SerializeField] private bool showGridInScene = true;
    [SerializeField] private Color gridColor = Color.cyan;

    [HideInInspector] public SpriteRenderer platformSpriterenderer;
    [HideInInspector] public PolygonCollider2D platformCollider;

    private void OnValidate()
    {
        platformSpriterenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<PolygonCollider2D>();
    }

    private void OnDrawGizmos()
    {
        if (!showGridInScene)
            return;

        Gizmos.color = gridColor;

        Bounds bounds = platformSpriterenderer.bounds;

        float startX = bounds.min.x;
        float startY = bounds.min.y;
        float width = bounds.size.x;
        float height = bounds.size.y;

        float cellWidth = width / Mathf.Max(1, gridColumns);
        float cellHeight = height / Mathf.Max(1, gridRows);

        for (int col = 0; col <= gridColumns; col++)
        {
            float xPos = startX + (col * cellWidth);
            Vector3 topPoint = new Vector3(xPos, bounds.max.y, bounds.center.z);
            Vector3 bottomPoint = new Vector3(xPos, startY, bounds.center.z);
            Gizmos.DrawLine(topPoint, bottomPoint);
        }

        for (int row = 0; row <= gridRows; row++)
        {
            float yPos = startY + (row * cellHeight);
            Vector3 leftPoint = new Vector3(startX, yPos, bounds.center.z);
            Vector3 rightPoint = new Vector3(bounds.max.x, yPos, bounds.center.z);
            Gizmos.DrawLine(leftPoint, rightPoint);
        }
    }
}