using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemPiece : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private ItemShapeData shapeData;
    [SerializeField] private Image itemDisplayImage;

    private GameGridLayout gridLayout;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 spawnInventoryPosition;

    private int currentRotation = 0; 
    private bool isPlaced = false;
    private Vector2Int placedGridPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        gridLayout = FindFirstObjectByType<GameGridLayout>();
    }

    public void InitializeItem(ItemShapeData data, float cellSize, float spacing)
    {
        shapeData = data;

        // Disable root single image if present
        if (itemDisplayImage != null)
        {
            itemDisplayImage.enabled = false;
        }

        // Clear any old child blocks
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // Force center anchors and pivot
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;

        // Compute unrotated bounding dimensions
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (Vector2Int pos in shapeData.relativeGridPositions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        int widthInCells = (maxX - minX) + 1;
        int heightInCells = (maxY - minY) + 1;

        float width = (widthInCells * cellSize) + ((widthInCells - 1) * spacing);
        float height = (heightInCells * cellSize) + ((heightInCells - 1) * spacing);
        rectTransform.sizeDelta = new Vector2(width, height);

        float boxCenterX = width * 0.5f;
        float boxCenterY = height * 0.5f;

        Color pieceColor = GetShapeColor(shapeData);

        // Spawn a distinct colored block tile for each cell position
        foreach (Vector2Int pos in shapeData.relativeGridPositions)
        {
            GameObject blockObj = new GameObject($"Block_{pos.x}_{pos.y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blockObj.transform.SetParent(transform, false);

            RectTransform blockRect = blockObj.GetComponent<RectTransform>();
            blockRect.anchorMin = blockRect.anchorMax = blockRect.pivot = new Vector2(0.5f, 0.5f);
            blockRect.sizeDelta = new Vector2(cellSize, cellSize);

            float cellCenterX = (pos.x - minX) * (cellSize + spacing) + (cellSize * 0.5f);
            float cellCenterY = (pos.y - minY) * (cellSize + spacing) + (cellSize * 0.5f);

            blockRect.anchoredPosition = new Vector2(cellCenterX - boxCenterX, cellCenterY - boxCenterY);

            Image blockImg = blockObj.GetComponent<Image>();
            blockImg.color = pieceColor;
            blockImg.raycastTarget = true;
        }
    }

    private Color GetShapeColor(ItemShapeData data)
    {

        string name = data.itemName.ToLower();
        if (name.Contains("shirt")) return new Color(0.2f, 0.6f, 1f, 1f);      // Vibrant Blue
        if (name.Contains("pant")) return new Color(0.92f, 0.3f, 0.3f, 1f);    // Coral / Crimson
        if (name.Contains("underwear")) return new Color(0.6f, 0.35f, 0.9f, 1f);// Violet Purple
        if (name.Contains("sock")) return new Color(0.15f, 0.78f, 0.45f, 1f);  // Emerald Green
        if (name.Contains("wallet")) return new Color(0.96f, 0.65f, 0.12f, 1f);// Amber Gold
        if (name.Contains("toilet")) return new Color(0.95f, 0.4f, 0.7f, 1f);  // Rose Pink

        int count = data.relativeGridPositions.Count;
        switch (count)
        {
            case 1: return new Color(0.95f, 0.4f, 0.7f, 1f);
            case 2: return new Color(0.96f, 0.65f, 0.12f, 1f);
            case 3: return new Color(0.15f, 0.78f, 0.45f, 1f);
            case 4: return new Color(0.6f, 0.35f, 0.9f, 1f);
            case 5: return new Color(0.2f, 0.6f, 1f, 1f);
            default: return new Color(0.92f, 0.3f, 0.3f, 1f);
        }
    }

    public void GetRotatedBounds(out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue; maxX = int.MinValue;
        minY = int.MaxValue; maxY = int.MinValue;

        foreach (Vector2Int pos in shapeData.relativeGridPositions)
        {
            Vector2Int rotated = GameGridLayout.RotateOffset(pos, currentRotation);
            if (rotated.x < minX) minX = rotated.x;
            if (rotated.x > maxX) maxX = rotated.x;
            if (rotated.y < minY) minY = rotated.y;
            if (rotated.y > maxY) maxY = rotated.y;
        }
    }

    private float inventoryScale = 1f;

    public void SetInventoryScale(float scale)
    {
        inventoryScale = scale;
        if (!isPlaced)
        {
            rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private bool isBeingDragged = false;

    public void RotatePiece()
    {
        currentRotation = (currentRotation + 90) % 360;
        rectTransform.Rotate(0, 0, -90);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
    }

    void Update()
    {
        // Rotate with 'R' key or Right-Click while being dragged
        if (isBeingDragged)
        {
            bool rotateRequested = false;

            if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1))
            {
                rotateRequested = true;
            }

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                rotateRequested = true;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                rotateRequested = true;
#endif

            if (rotateRequested)
            {
                RotatePiece();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Right-click in inventory (when not dragging)
        if (eventData.button == PointerEventData.InputButton.Right && !isPlaced)
        {
            RotatePiece();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isBeingDragged = true;

        if (originalParent == null)
        {
            originalParent = transform.parent;
            spawnInventoryPosition = rectTransform.position;
        }

        // Full scale when dragging to match grid
        rectTransform.localScale = Vector3.one;

        // Move to top of UI hierarchy while dragging
        transform.SetAsLastSibling();

        if (isPlaced)
        {
            gridLayout.SetItemOccupancy(shapeData, placedGridPos.x, placedGridPos.y, currentRotation, false);
            isPlaced = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        canvasGroup.blocksRaycasts = false;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isBeingDragged = false;
        canvasGroup.blocksRaycasts = true;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (gridLayout.GetGridPositionFromMouse(Input.mousePosition, cam, out int mouseGridX, out int mouseGridY))
        {
            GetRotatedBounds(out int minX, out int maxX, out int minY, out int maxY);

            // 1. Calculate center offset so the piece drops where the cursor is holding it
            int centerOffsetX = Mathf.RoundToInt((minX + maxX) / 2f);
            int centerOffsetY = Mathf.RoundToInt((minY + maxY) / 2f);

            int targetX = mouseGridX - centerOffsetX;
            int targetY = mouseGridY - centerOffsetY;

            // 2. Smart placement search: test centered target, direct mouse, and adjacent offsets
            int bestX = -1, bestY = -1;
            Vector2Int[] searchOffsets = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(centerOffsetX, centerOffsetY), // Direct mouse cell
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(0, 1),
                new Vector2Int(-1, -1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, 1),
                new Vector2Int(1, 1)
            };

            foreach (var offset in searchOffsets)
            {
                int testX = targetX + offset.x;
                int testY = targetY + offset.y;

                if (gridLayout.CanPlaceItem(shapeData, testX, testY, currentRotation))
                {
                    bestX = testX;
                    bestY = testY;
                    break;
                }
            }

            // 3. Place piece and snap to the exact visual center of the occupied bounding box
            if (bestX != -1)
            {
                gridLayout.SetItemOccupancy(shapeData, bestX, bestY, currentRotation, true);
                isPlaced = true;
                placedGridPos = new Vector2Int(bestX, bestY);

                // Multi-cell bounding box center calculation
                Vector3 minCellPos = gridLayout.GetCellWorldPosition(bestX + minX, bestY + minY);
                Vector3 maxCellPos = gridLayout.GetCellWorldPosition(bestX + maxX, bestY + maxY);
                rectTransform.position = (minCellPos + maxCellPos) * 0.5f;
                rectTransform.localScale = Vector3.one;

                // Play block placement sound effect
                if (AudioManager.Instance != null) AudioManager.Instance.PlayBlockPlace();

                // Notify Game Manager to check win condition
                GameManager gm = FindFirstObjectByType<GameManager>();
                if (gm != null) gm.CheckBagState();

                return;
            }
        }

        // If placement failed, return to inventory position and scale
        rectTransform.localScale = new Vector3(inventoryScale, inventoryScale, 1f);
        rectTransform.position = spawnInventoryPosition;
    }
}
