using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Available Shapes Library")]
    [SerializeField] private List<ItemShapeData> shapeLibrary;

    [Header("References")]
    [SerializeField] private GameObject itemPiecePrefab;
    [SerializeField] private Transform itemSpawnContainer;
    [SerializeField] private GameGridLayout gridLayout;

    public void StartNewPuzzle()
    {
        if (gridLayout == null) gridLayout = FindFirstObjectByType<GameGridLayout>();
        if (itemSpawnContainer == null)
        {
            GameObject container = GameObject.Find("ItemsContainer");
            if (container != null) itemSpawnContainer = container.transform;
        }
        // Clear previous items in spawn container
        foreach (Transform child in itemSpawnContainer)
        {
            Destroy(child.gameObject);
        }

        gridLayout.ClearGrid();

        // 1. Generate guaranteed solvable tiling
        List<ItemShapeData> solvableSet = GenerateSolvableSet();

        // 2. Shuffle order so it's a fun packing challenge
        for (int i = 0; i < solvableSet.Count; i++)
        {
            ItemShapeData temp = solvableSet[i];
            int randIndex = Random.Range(i, solvableSet.Count);
            solvableSet[i] = solvableSet[randIndex];
            solvableSet[randIndex] = temp;
        }

        // 3. Instantiate draggable items and distribute them evenly inside the container bounds
        RectTransform containerRect = itemSpawnContainer.GetComponent<RectTransform>();
        
        float containerWidth = (containerRect != null && containerRect.rect.width > 0) ? containerRect.rect.width : 810f;
        float containerHeight = (containerRect != null && containerRect.rect.height > 0) ? containerRect.rect.height : 860f;

        float paddingX = 40f;
        float paddingY = 40f;

        float usableWidth = containerWidth - (paddingX * 2);
        float usableHeight = containerHeight - (paddingY * 2);

        int totalItems = solvableSet.Count;
        int cols = 3;
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / cols));

        float slotWidth = usableWidth / cols;
        float slotHeight = usableHeight / rows;

        // Position coordinates relative to container center (0, 0)
        float startX = (-containerWidth * 0.5f) + paddingX;
        float startY = (containerHeight * 0.5f) - paddingY;

        // Calculate a comfortable scale so large 3x3 pieces fit neatly in slots
        float maxPieceDimension = (gridLayout.CellSize * 3f) + (gridLayout.CellSpacing * 2f); // ~370px
        float maxSlotDimension = Mathf.Min(slotWidth, slotHeight);
        float autoScale = Mathf.Clamp((maxSlotDimension / maxPieceDimension) * 0.9f, 0.45f, 0.6f);

        for (int i = 0; i < totalItems; i++)
        {
            ItemShapeData shape = solvableSet[i];
            GameObject obj = Instantiate(itemPiecePrefab, itemSpawnContainer);
            ItemPiece piece = obj.GetComponent<ItemPiece>();
            piece.InitializeItem(shape, gridLayout.CellSize, gridLayout.CellSpacing);

            RectTransform pieceRect = obj.GetComponent<RectTransform>();
            int c = i % cols;
            int r = i / cols;

            // Center each piece in its allocated grid slot
            float posX = startX + (c * slotWidth) + (slotWidth * 0.5f);
            float posY = startY - (r * slotHeight) - (slotHeight * 0.5f);

            pieceRect.anchoredPosition = new Vector2(posX, posY);
            piece.SetInventoryScale(autoScale);
        }
    }

    private List<ItemShapeData> GenerateSolvableSet()
    {
        int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bool[,] simGrid = new bool[GameGridLayout.COLUMNS, GameGridLayout.ROWS];
            List<ItemShapeData> chosenShapes = new List<ItemShapeData>();

            if (FillGridRecursive(simGrid, chosenShapes))
            {
                return chosenShapes;
            }
        }

        Debug.LogWarning("Tiling generation fallback to default shapes.");
        return new List<ItemShapeData>(shapeLibrary);
    }

    private bool FillGridRecursive(bool[,] grid, List<ItemShapeData> chosen)
    {
        // Find next empty cell
        int freeX = -1, freeY = -1;
        for (int y = 0; y < GameGridLayout.ROWS; y++)
        {
            for (int x = 0; x < GameGridLayout.COLUMNS; x++)
            {
                if (!grid[x, y])
                {
                    freeX = x;
                    freeY = y;
                    break;
                }
            }
            if (freeX != -1) break;
        }

        // If no empty cells remain, 8x8 is 100% full and solved!
        if (freeX == -1) return true;

        // Shuffle shape library and rotations
        List<ItemShapeData> shuffledShapes = new List<ItemShapeData>(shapeLibrary);
        Shuffle(shuffledShapes);

        int[] rotations = { 0, 90, 180, 270 };

        foreach (var shape in shuffledShapes)
        {
            Shuffle(rotations);
            foreach (int rot in rotations)
            {
                // Find anchor offsets so one of the shape's cells covers (freeX, freeY)
                foreach (Vector2Int cellOffset in shape.relativeGridPositions)
                {
                    Vector2Int rotCell = GameGridLayout.RotateOffset(cellOffset, rot);
                    int anchorX = freeX - rotCell.x;
                    int anchorY = freeY - rotCell.y;

                    if (CanFitInSimGrid(grid, shape, anchorX, anchorY, rot))
                    {
                        // Place piece
                        SetSimGrid(grid, shape, anchorX, anchorY, rot, true);
                        chosen.Add(shape);

                        if (FillGridRecursive(grid, chosen))
                            return true;

                        // Backtrack
                        SetSimGrid(grid, shape, anchorX, anchorY, rot, false);
                        chosen.RemoveAt(chosen.Count - 1);
                    }
                }
            }
        }

        return false;
    }

    private bool CanFitInSimGrid(bool[,] grid, ItemShapeData item, int anchorX, int anchorY, int rot)
    {
        foreach (Vector2Int pos in item.relativeGridPositions)
        {
            Vector2Int rotated = GameGridLayout.RotateOffset(pos, rot);
            int targetX = anchorX + rotated.x;
            int targetY = anchorY + rotated.y;

            if (targetX < 0 || targetX >= GameGridLayout.COLUMNS || targetY < 0 || targetY >= GameGridLayout.ROWS)
                return false;

            if (grid[targetX, targetY])
                return false;
        }
        return true;
    }

    private void SetSimGrid(bool[,] grid, ItemShapeData item, int anchorX, int anchorY, int rot, bool value)
    {
        foreach (Vector2Int pos in item.relativeGridPositions)
        {
            Vector2Int rotated = GameGridLayout.RotateOffset(pos, rot);
            grid[anchorX + rotated.x, anchorY + rotated.y] = value;
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }
}
