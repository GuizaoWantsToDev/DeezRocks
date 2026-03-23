using System.Collections.Generic;
using UnityEngine;

public class PlatformDivider : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridColumns = 10;
    public int gridRows = 4;
    [Range(0f, 1f)] public float randomness = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Texture2D sourceTexture;
    private Vector2Int[,] seedPoints;

    private int imageWidth;
    private int imageHeight;
    private int cellWidth;
    private int cellHeight;
    private float pixelsPerUnit;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        sourceTexture = spriteRenderer.sprite.texture;

        imageWidth = Mathf.RoundToInt(spriteRenderer.sprite.rect.width);
        imageHeight = Mathf.RoundToInt(spriteRenderer.sprite.rect.height);
        pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;

        cellWidth = Mathf.Max(1, imageWidth / gridColumns);
        cellHeight = Mathf.Max(1, imageHeight / gridRows);

        GenerateSeedPoints();
        DividePlatform();

        spriteRenderer.enabled = false;
    }

    private void GenerateSeedPoints()
    {
        seedPoints = new Vector2Int[gridColumns, gridRows];

        for (int col = 0; col < gridColumns; col++)
        {
            for (int row = 0; row < gridRows; row++)
            {
                // Flat-topped hexagon offset (zigzag on Y axis)
                float offsetY = (col % 2 != 0) ? cellHeight / 2f : 0f;

                float centerX = (col * cellWidth) + (cellWidth / 2f);
                float centerY = (row * cellHeight) + offsetY + (cellHeight / 2f);

                float maxJitterX = (cellWidth / 2f) * randomness;
                float maxJitterY = (cellHeight / 2f) * randomness;

                int randomX = Mathf.RoundToInt(centerX + Random.Range(-maxJitterX, maxJitterX));
                int randomY = Mathf.RoundToInt(centerY + Random.Range(-maxJitterY, maxJitterY));

                randomX = Mathf.Clamp(randomX, 0, imageWidth - 1);
                randomY = Mathf.Clamp(randomY, 0, imageHeight - 1);

                seedPoints[col, row] = new Vector2Int(randomX, randomY);
            }
        }
    }

    private void DividePlatform()
    {
        var pixelsByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();

        for (int x = 0; x < imageWidth; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                int gridX = Mathf.Clamp(x / cellWidth, 0, gridColumns - 1);
                int gridY = Mathf.Clamp(y / cellHeight, 0, gridRows - 1);

                float shortestDist = Mathf.Infinity;
                Vector2Int closestSeed = new Vector2Int();

                // Check 9 neighboring cells
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighborX = gridX + offsetX;
                        int neighborY = gridY + offsetY;

                        if (neighborX < 0 || neighborY < 0 || neighborX >= gridColumns || neighborY >= gridRows)
                            continue;

                        float dist = Vector2.Distance(new Vector2(x, y), (Vector2)seedPoints[neighborX, neighborY]);

                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            closestSeed = new Vector2Int(neighborX, neighborY);
                        }
                    }
                }

                if (!pixelsByPiece.ContainsKey(closestSeed))
                    pixelsByPiece[closestSeed] = new List<Vector2Int>();

                pixelsByPiece[closestSeed].Add(new Vector2Int(x, y));
            }
        }

        CreatePieceObjects(pixelsByPiece);
    }

    private void CreatePieceObjects(Dictionary<Vector2Int, List<Vector2Int>> pixelsByPiece)
    {
        float sheetX = spriteRenderer.sprite.rect.x;
        float sheetY = spriteRenderer.sprite.rect.y;

        Vector2 pivotPercent = new Vector2(
            spriteRenderer.sprite.pivot.x / spriteRenderer.sprite.rect.width,
            spriteRenderer.sprite.pivot.y / spriteRenderer.sprite.rect.height
        );

        foreach (var piece in pixelsByPiece)
        {
            List<Vector2Int> pixels = piece.Value;
            if (pixels.Count == 0) continue;

            int minX = imageWidth, maxX = 0, minY = imageHeight, maxY = 0;
            foreach (Vector2Int p in pixels)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }

            int pieceWidth = maxX - minX + 1;
            int pieceHeight = maxY - minY + 1;

            Texture2D pieceTex = new Texture2D(pieceWidth, pieceHeight);
            pieceTex.filterMode = FilterMode.Point; // Hard edges
            pieceTex.wrapMode = TextureWrapMode.Clamp;

            // 1. Transparent ghost background (Hard cut)
            for (int x = 0; x < pieceWidth; x++)
            {
                for (int y = 0; y < pieceHeight; y++)
                {
                    int srcX = Mathf.RoundToInt(sheetX) + minX + x;
                    int srcY = Mathf.RoundToInt(sheetY) + minY + y;
                    Color c = sourceTexture.GetPixel(srcX, srcY);
                    c.a = 0f;
                    pieceTex.SetPixel(x, y, c);
                }
            }

            // 2. Solid visible pixels (Hard cut, no bilinear blurring)
            foreach (Vector2Int p in pixels)
            {
                int srcX = Mathf.RoundToInt(sheetX) + p.x;
                int srcY = Mathf.RoundToInt(sheetY) + p.y;
                pieceTex.SetPixel(p.x - minX, p.y - minY, sourceTexture.GetPixel(srcX, srcY));
            }
            pieceTex.Apply();

            Sprite pieceSprite = Sprite.Create(pieceTex, new Rect(0, 0, pieceWidth, pieceHeight), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);

            GameObject pieceObj = new GameObject($"Piece_{piece.Key.x}_{piece.Key.y}");
            pieceObj.transform.SetParent(this.transform);
            pieceObj.layer = LayerMask.NameToLayer("PlatformPiece");

            float centerX = minX + (pieceWidth / 2f);
            float centerY = minY + (pieceHeight / 2f);

            float localX = (centerX - (pivotPercent.x * imageWidth)) / pixelsPerUnit;
            float localY = (centerY - (pivotPercent.y * imageHeight)) / pixelsPerUnit;

            pieceObj.transform.localPosition = new Vector3(localX, localY, 0);

            SpriteRenderer renderer = pieceObj.AddComponent<SpriteRenderer>();
            renderer.sprite = pieceSprite;

            pieceObj.AddComponent<PolygonCollider2D>();
            pieceObj.AddComponent<PieceCleanUp>();

            // --- Gap Filler: Creates a background copy to hide seams ---
            GameObject gapFiller = new GameObject("GapFiller");
            gapFiller.transform.SetParent(pieceObj.transform);
            gapFiller.transform.localPosition = new Vector3(-0.01f, -0.01f, 0.01f);

            SpriteRenderer fillerRenderer = gapFiller.AddComponent<SpriteRenderer>();
            fillerRenderer.sprite = pieceSprite;
            fillerRenderer.sortingLayerID = renderer.sortingLayerID;
            fillerRenderer.sortingOrder = renderer.sortingOrder - 1; // Renders behind the main piece
        }
    }
    private void OnValidate()
    {
        if (gridColumns <= 0) gridColumns = 1;
        if (gridRows <= 0) gridRows = 1;
    }
}