using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlatformDivider : MonoBehaviour
{
    [Header("Grid Setup")]
    public int gridColumns = 10;
    public int gridRows = 4;

    [Header("Visuals")]
    [Tooltip("Multiplier for virtual resolution. 2 or 3 smoothly blends diagonal cuts to remove jagged edges.")]
    [Range(1, 4)]
    public int cutSmoothness = 2;

    private SpriteRenderer platformRenderer;
    private Texture2D platformTexture;
    private Vector2Int[,] voronoiSeedPoints;

    private int imageWidth;
    private int imageHeight;
    private int cellWidthPixels;
    private int cellHeightPixels;
    private float pixelsPerUnit;

    void Start()
    {
        platformRenderer = GetComponent<SpriteRenderer>();;

        platformTexture = platformRenderer.sprite.texture;

        // --- 1. GETTING THE RECTANGLE SIZE & SMOOTHNESS ---
        // We read the exact width and height of the original artwork's rectangle in pixels.
        // Then, we multiply these dimensions by our 'cutSmoothness' multiplier.
        // This creates a high-resolution virtual canvas, acting as Anti-Aliasing to make diagonal cuts perfectly smooth.
        imageWidth = Mathf.RoundToInt(platformRenderer.sprite.rect.width) * cutSmoothness;
        imageHeight = Mathf.RoundToInt(platformRenderer.sprite.rect.height) * cutSmoothness;
        pixelsPerUnit = platformRenderer.sprite.pixelsPerUnit * cutSmoothness;

        // --- 2. HOW THE GRID WORKS INSIDE THE RECTANGLE ---
        // We divide the total image width and height by our desired columns and rows.
        // This tells us exactly how many pixels wide and tall each invisible grid cell is.
        // Mathf.Max ensures a cell is at least 1 pixel wide to prevent division-by-zero errors.
        cellWidthPixels = Mathf.Max(1, imageWidth / gridColumns);
        cellHeightPixels = Mathf.Max(1, imageHeight / gridRows);

        GenerateSeedPoints();
        DividePlatform();

        platformRenderer.enabled = false;
    }

    private void GenerateSeedPoints()
    {
        voronoiSeedPoints = new Vector2Int[gridColumns, gridRows];

        for (int x = 0; x < gridColumns; x++)
        {
            for (int y = 0; y < gridRows; y++)
            {
                int randomPosX = (x * cellWidthPixels) + Random.Range(0, cellWidthPixels);
                int fixedPosY = (y * cellHeightPixels) + Random.Range(0, cellWidthPixels); //(cellHeightPixels / 2);

                // Save this seed point into our array for this specific cell
                voronoiSeedPoints[x, y] = new Vector2Int(randomPosX, fixedPosY);
            }
        }
    }

    private void DividePlatform()
    {
        // This dictionary is crucial: it groups pixels together. 
        // The Key is the Seed Point coordinates, and the Value is the list of all pixels belonging to that seed.
        Dictionary<Vector2Int, List<Vector2Int>> pixelsGroupedByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();

        // --- 5. CREATING THE VORONOI SHAPES ---
        // We use a nested loop to scan every single pixel of our high-resolution virtual image (width * height).
        for (int x = 0; x < imageWidth; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                // Find out which grid cell this specific pixel lives in
                int currentGridX = Mathf.Clamp(x / cellWidthPixels, 0, gridColumns - 1);
                int currentGridY = Mathf.Clamp(y / cellHeightPixels, 0, gridRows - 1);

                float shortestDistance = Mathf.Infinity;
                Vector2Int closestSeedCoordinates = new Vector2Int();

                // --- 6. SHAPE SELECTION (THE 9-NEIGHBOR RULE) ---
                // To figure out which shape this pixel belongs to, we don't check all seeds (which would cause lag).
                // We only check the distance to the seed in the pixel's current cell and its 8 immediate neighbor cells.
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighborCellX = currentGridX + offsetX;
                        int neighborCellY = currentGridY + offsetY;

                        // Skip if the neighbor cell we are trying to check is outside the platform's grid boundaries
                        if (neighborCellX < 0 || neighborCellY < 0 || neighborCellX >= gridColumns || neighborCellY >= gridRows) continue;

                        // Calculate the physical distance from our current pixel to this neighbor's seed point
                        float distanceToSeed = Vector2.Distance(
                            new Vector2(x, y),
                            new Vector2(voronoiSeedPoints[neighborCellX, neighborCellY].x, voronoiSeedPoints[neighborCellX, neighborCellY].y)
                        );

                        // If this seed is the closest one we've found so far, it becomes the new "owner" of this pixel
                        if (distanceToSeed < shortestDistance)
                        {
                            shortestDistance = distanceToSeed;
                            closestSeedCoordinates = new Vector2Int(neighborCellX, neighborCellY);
                        }
                    }
                }

                // Add this pixel to the list of the winning seed in our dictionary
                if (!pixelsGroupedByPiece.ContainsKey(closestSeedCoordinates))
                {
                    pixelsGroupedByPiece[closestSeedCoordinates] = new List<Vector2Int>();
                }
                pixelsGroupedByPiece[closestSeedCoordinates].Add(new Vector2Int(x, y));
            }
        }

        CreatePieceGameObjects(pixelsGroupedByPiece);
    }

    private void CreatePieceGameObjects(Dictionary<Vector2Int, List<Vector2Int>> pixelsGroupedByPiece)
    {
        int spriteStartX = Mathf.RoundToInt(platformRenderer.sprite.rect.x);
        int spriteStartY = Mathf.RoundToInt(platformRenderer.sprite.rect.y);
        Vector2 originalPivot = platformRenderer.sprite.pivot;

        // --- 7. SEPARATION AND CREATION OF UNIQUE GAMEOBJECTS ---
        // We iterate through every constructed Voronoi piece in our dictionary.
        foreach (var pieceData in pixelsGroupedByPiece)
        {
            Vector2Int pieceID = pieceData.Key;
            List<Vector2Int> piecePixels = pieceData.Value;

            if (piecePixels.Count == 0) continue;

            // Find the exact Bounding Box (the extreme left, right, top, and bottom edges) of this specific piece.
            // This ensures we only create a small texture as big as the piece itself, saving memory.
            int minX = imageWidth, maxX = 0;
            int minY = imageHeight, maxY = 0;

            foreach (Vector2Int pixel in piecePixels)
            {
                if (pixel.x < minX) minX = pixel.x;
                if (pixel.x > maxX) maxX = pixel.x;
                if (pixel.y < minY) minY = pixel.y;
                if (pixel.y > maxY) maxY = pixel.y;
            }

            int pieceWidth = maxX - minX + 1;
            int pieceHeight = maxY - minY + 1;

            Texture2D pieceTexture = new Texture2D(pieceWidth, pieceHeight);
            pieceTexture.filterMode = platformTexture.filterMode;
            pieceTexture.wrapMode = TextureWrapMode.Clamp;

            // Ghost Background logic: We paint the invisible areas with the artwork's original colors but set Alpha to 0.
            // This plays a huge role in the 'Smoothness' system, stopping Unity from generating dark outlines when blending the piece's edges.
            for (int x = 0; x < pieceWidth; x++)
            {
                for (int y = 0; y < pieceHeight; y++)
                {
                    float u = (spriteStartX + ((float)(minX + x) / cutSmoothness)) / platformTexture.width;
                    float v = (spriteStartY + ((float)(minY + y) / cutSmoothness)) / platformTexture.height;

                    Color ghostColor = platformTexture.GetPixelBilinear(u, v);
                    ghostColor.a = 0f;
                    pieceTexture.SetPixel(x, y, ghostColor);
                }
            }

            // Paint the actual visible pixels belonging to this piece on top of the ghost background
            foreach (Vector2Int pixel in piecePixels)
            {
                float u = (spriteStartX + ((float)pixel.x / cutSmoothness)) / platformTexture.width;
                float v = (spriteStartY + ((float)pixel.y / cutSmoothness)) / platformTexture.height;

                Color pixelColor = platformTexture.GetPixelBilinear(u, v);
                pieceTexture.SetPixel(pixel.x - minX, pixel.y - minY, pixelColor);
            }
            pieceTexture.Apply();

            // --- 8. TEXTURE TO SPRITE CONVERSION ---
            // We convert our painted Texture2D into a Unity Sprite. 
            // We use 'SpriteMeshType.FullRect' and '0' extrude to prevent Unity from adding invisible pixel margins around the piece, avoiding gaps.
            Sprite pieceSprite = Sprite.Create(pieceTexture, new Rect(0, 0, pieceWidth, pieceHeight), new Vector2(0.5f, 0.5f), pixelsPerUnit, 2, SpriteMeshType.Tight);


            GameObject pieceObject = new GameObject("PlatformPiece_" + pieceID.x + "_" + pieceID.y);
            pieceObject.transform.SetParent(this.transform);

            pieceObject.layer = LayerMask.NameToLayer("PlatformPiece");

            // Calculate the exact local position to reconstruct the platform perfectly, taking the original pivot into account.
            float localX = (minX + pieceWidth / 2f - (originalPivot.x * cutSmoothness)) / pixelsPerUnit;
            float localY = (minY + pieceHeight / 2f - (originalPivot.y * cutSmoothness)) / pixelsPerUnit;

            pieceObject.transform.localPosition = new Vector2(localX, localY);  

            SpriteRenderer pieceRenderer = pieceObject.AddComponent<SpriteRenderer>();
            pieceRenderer.sprite = pieceSprite;
            
            pieceObject.AddComponent<PolygonCollider2D>();
            pieceObject.AddComponent<PieceCleanUp>();
        }
    }

    private void OnValidate()
    {
        if (gridColumns <= 0)
        {
            gridColumns = 1;
            Debug.LogWarning("GridColumns is 0 or less");
        }
        if (gridRows <= 0)
        {
            gridRows = 1;
            Debug.LogError("GridRows is 0 or less");
        }
    }
}