using System.Collections.Generic;
using System.Reflection; // O PÉ DE CABRA!
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class PlatformToMeshDivider : MonoBehaviour
{
    [Header("Configurações")]
    public int gridColumns = 10;
    public int gridRows = 4;
    public string pieceLayer = "PlatformPiece";

    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D mainCollider;
    private ShadowCaster2D originalShadow;
    private Texture2D sourceTexture;
    private Vector2Int[,] seedPoints;

    private int imageWidth, imageHeight, cellWidth, cellHeight;
    private float pixelsPerUnit;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<PolygonCollider2D>();
        originalShadow = GetComponent<ShadowCaster2D>();
        sourceTexture = spriteRenderer.sprite.texture;

        imageWidth = Mathf.RoundToInt(spriteRenderer.sprite.rect.width);
        imageHeight = Mathf.RoundToInt(spriteRenderer.sprite.rect.height);
        pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;

        cellWidth = Mathf.Max(1, imageWidth / gridColumns);
        cellHeight = Mathf.Max(1, imageHeight / gridRows);

        GenerateSeedPoints();
        DividePlatform();

        // Limpeza final: Desligar tudo da plataforma original!
        spriteRenderer.enabled = false;
        mainCollider.enabled = false;
        if (originalShadow != null) originalShadow.enabled = false;
    }

    private void GenerateSeedPoints()
    {
        seedPoints = new Vector2Int[gridColumns, gridRows];

        // Randomness removido! Usamos o valor máximo (100% de desvio) diretamente
        float halfW = cellWidth / 2f;
        float halfH = cellHeight / 2f;

        for (int col = 0; col < gridColumns; col++)
        {
            float offsetY = (col % 2 != 0) ? halfH : 0f;

            for (int row = 0; row < gridRows; row++)
            {
                float centerX = (col * cellWidth) + halfW;
                float centerY = (row * cellHeight) + offsetY + halfH;

                int randomX = Mathf.Clamp(Mathf.RoundToInt(centerX + Random.Range(-halfW, halfW)), 0, imageWidth - 1);
                int randomY = Mathf.Clamp(Mathf.RoundToInt(centerY + Random.Range(-halfH, halfH)), 0, imageHeight - 1);

                seedPoints[col, row] = new Vector2Int(randomX, randomY);
            }
        }
    }

    private void DividePlatform()
    {
        var pixelsByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();
        Vector2 pivot = spriteRenderer.sprite.pivot;

        for (int x = 0; x < imageWidth; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                Vector3 localPos = new Vector3((x - pivot.x) / pixelsPerUnit, (y - pivot.y) / pixelsPerUnit, 0);
                Vector3 worldPos = transform.TransformPoint(localPos);

                if (!mainCollider.OverlapPoint(worldPos)) continue;

                int gridX = Mathf.Clamp(x / cellWidth, 0, gridColumns - 1);
                int gridY = Mathf.Clamp(y / cellHeight, 0, gridRows - 1);

                float shortestDist = Mathf.Infinity;
                Vector2Int closestSeed = new Vector2Int();

                for (int offsetX = -2; offsetX <= 2; offsetX++)
                {
                    for (int offsetY = -2; offsetY <= 2; offsetY++)
                    {
                        int neighborX = gridX + offsetX;
                        int neighborY = gridY + offsetY;

                        if (neighborX < 0 || neighborY < 0 || neighborX >= gridColumns || neighborY >= gridRows) continue;

                        float dist = Vector2.Distance(new Vector2(x, y), (Vector2)seedPoints[neighborX, neighborY]);

                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            closestSeed = new Vector2Int(neighborX, neighborY);
                        }
                    }
                }

                if (!pixelsByPiece.ContainsKey(closestSeed)) pixelsByPiece[closestSeed] = new List<Vector2Int>();
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

        int targetLayer = LayerMask.NameToLayer(pieceLayer);

        foreach (var piece in pixelsByPiece)
        {
            List<Vector2Int> pixels = piece.Value;
            if (pixels.Count < 20) continue;

            int minX = imageWidth, maxX = 0, minY = imageHeight, maxY = 0;
            foreach (Vector2Int p in pixels)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }

            int pieceWidth = maxX - minX + 1;
            int pieceHeight = maxY - minY + 1;

            Texture2D pieceTex = new Texture2D(pieceWidth, pieceHeight, TextureFormat.RGBA32, false);
            pieceTex.filterMode = FilterMode.Bilinear;
            pieceTex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < pieceWidth; x++)
            {
                for (int y = 0; y < pieceHeight; y++)
                {
                    pieceTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }

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
            pieceObj.layer = targetLayer;

            float centerX = minX + (pieceWidth / 2f);
            float centerY = minY + (pieceHeight / 2f);
            float localX = (centerX - (pivotPercent.x * imageWidth)) / pixelsPerUnit;
            float localY = (centerY - (pivotPercent.y * imageHeight)) / pixelsPerUnit;

            pieceObj.transform.localPosition = new Vector3(localX, localY, 0);
            pieceObj.transform.localScale = Vector3.one;

            SpriteRenderer renderer = pieceObj.AddComponent<SpriteRenderer>();
            renderer.sprite = pieceSprite;

            PolygonCollider2D poly = pieceObj.AddComponent<PolygonCollider2D>();

            if (poly.pathCount == 0 || poly.points.Length == 5)
            {
                Destroy(pieceObj);
                continue;
            }

            GameObject gapFiller = new GameObject("GapFiller");
            gapFiller.transform.SetParent(pieceObj.transform);
            gapFiller.transform.localPosition = new Vector3(-0.02f, -0.02f, 0f);

            SpriteRenderer fillerRenderer = gapFiller.AddComponent<SpriteRenderer>();
            fillerRenderer.sprite = pieceSprite;
            fillerRenderer.sortingLayerID = renderer.sortingLayerID;
            fillerRenderer.sortingOrder = renderer.sortingOrder - 1;

            // --- SOMBRAS NINJA COM REFLECTION ---
            if (originalShadow != null)
            {
                ShadowCaster2D shadowCaster = pieceObj.AddComponent<ShadowCaster2D>();

                shadowCaster.castsShadows = originalShadow.castsShadows;
                shadowCaster.selfShadows = originalShadow.selfShadows;

                // 1. Extraímos os pontos do nosso colisor para Vector3
                Vector3[] shadowPath = new Vector3[poly.points.Length];
                for (int i = 0; i < poly.points.Length; i++)
                {
                    shadowPath[i] = poly.points[i];
                }

                // 2. REFLECTION: Injetamos a nossa forma e malha nova!
                FieldInfo shapeField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo onEnableMethod = typeof(ShadowCaster2D).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);

                if (shapeField != null && meshField != null && onEnableMethod != null)
                {
                    shapeField.SetValue(shadowCaster, shadowPath);
                    meshField.SetValue(shadowCaster, new Mesh());
                    onEnableMethod.Invoke(shadowCaster, null);
                }
            }
        }
    }
}