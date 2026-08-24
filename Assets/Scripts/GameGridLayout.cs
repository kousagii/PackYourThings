using UnityEngine;

public class GameGridLayout : MonoBehaviour
{
    public const int COLUMNS = 8;
    public const int ROWS = 8;

    [Header("Grid Configuration")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private RectTransform bagGridRect;

    [SerializeField] private float cellSizeInPixels = 120f;
    [SerializeField] private float cellSpacing = 5f;

    private bool[,] gridSlots = new bool[COLUMNS, ROWS];
    private RectTransform[,] cellTransforms = new RectTransform[COLUMNS, ROWS];

    public float CellSize => cellSizeInPixels;
    public float CellSpacing => cellSpacing;

    void Awake()
    {
        if (bagGridRect == null) bagGridRect = GetComponent<RectTransform>();
        GenerateGridVisuals();
    }

    private void GenerateGridVisuals()
    {
        foreach (Transform child in bagGridRect)
        {
            Destroy(child.gameObject);
        }

        for (int y = ROWS - 1; y >= 0; y--)
        {
            for (int x = 0; x < COLUMNS; x++) 
            {
                GameObject newCell = Instantiate(cellPrefab, bagGridRect);
                cellTransforms[x, y] = newCell.GetComponent<RectTransform>();
            }
        }
    }

    public bool CanPlaceItem(ItemShapeData item, int gridX, int gridY, int rotationDegrees)
    {
        foreach (Vector2Int pos in item.relativeGridPositions)
        {
            Vector2Int rotated = RotateOffset(pos, rotationDegrees);
            int targetX = gridX + rotated.x;
            int targetY = gridY + rotated.y;

            if (targetX < 0 || targetX >= COLUMNS || targetY < 0 || targetY >= ROWS)
                return false;

            if (gridSlots[targetX, targetY])
                return false;
        }
        return true;
    }

    public void SetItemOccupancy(ItemShapeData item, int gridX, int gridY, int rotationDegrees, bool isOccupied)
    {
        foreach (Vector2Int pos in item.relativeGridPositions)
        {
            Vector2Int rotated = RotateOffset(pos, rotationDegrees);
            int targetX = gridX + rotated.x;
            int targetY = gridY + rotated.y;

            if (targetX >= 0 && targetX < COLUMNS && targetY >= 0 && targetY < ROWS)
            {
                gridSlots[targetX, targetY] = isOccupied;
            }
        }
    }

    public bool GetGridPositionFromMouse(Vector2 screenMousePos, Camera cam, out int outX, out int outY)
    {
        outX = -1;
        outY = -1;
        float closestDistSqr = float.MaxValue;
        float maxSnapDist = (cellSizeInPixels + cellSpacing) * 1.2f;
        float maxSnapDistSqr = maxSnapDist * maxSnapDist;

        for (int x = 0; x < COLUMNS; x++)
        {
            for (int y = 0; y < ROWS; y++)
            {
                if (cellTransforms[x, y] == null) continue;

                Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(cam, cellTransforms[x, y].position);
                float distSqr = (cellScreenPos - screenMousePos).sqrMagnitude;

                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    outX = x;
                    outY = y;
                }
            }
        }

        // Return true if mouse is close enough to any grid cell
        return (outX != -1 && outY != -1 && closestDistSqr <= maxSnapDistSqr);
    }

    public Vector3 GetCellWorldPosition(int gridX, int gridY)
    {
        if (gridX >= 0 && gridX < COLUMNS && gridY >= 0 && gridY < ROWS)
        {
            if (cellTransforms[gridX, gridY] != null)
                return cellTransforms[gridX, gridY].position;
        }

        return bagGridRect.position;
    }

    public bool IsBagFull()
    {
        for (int x = 0; x < COLUMNS; x++)
        {
            for (int y = 0; y < ROWS; y++)
            {
                if (!gridSlots[x, y]) return false;
            }
        }
        return true;
    }

    public void ClearGrid()
    {
        for (int x = 0; x < COLUMNS; x++)
            for (int y = 0; y < ROWS; y++)
                gridSlots[x, y] = false;
    }1

    public static Vector2Int RotateOffset(Vector2Int point, int angle)
    {
        int norm = (angle % 360 + 360) % 360;
        switch (norm)
        {
            case 90:  return new Vector2Int(point.y, -point.x);
            case 180: return new Vector2Int(-point.x, -point.y);
            case 270: return new Vector2Int(-point.y, point.x);
            default:  return point;
        }
    }
}
