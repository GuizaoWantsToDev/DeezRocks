using System.Collections.Generic;
using UnityEngine;

public class PlatformDivider : MonoBehaviour
{
    [Header("Configuração da Grelha")]
    public int gridColumns = 10;
    public int gridRows = 4;

    private SpriteRenderer parentSpriteRenderer;
    private Texture2D sourceTexture;
    private Vector2Int[,] voronoiSeedPoints;

    private int totalImageWidth;
    private int totalImageHeight;
    private int cellWidthPixels;
    private int cellHeightPixels;
    private float pixelsPerUnit;

    void Start()
    {
        parentSpriteRenderer = GetComponent<SpriteRenderer>();
        sourceTexture = parentSpriteRenderer.sprite.texture;

        // --- 1. DIMENSÕES REAIS (Escala 1:1) ---
        totalImageWidth = Mathf.RoundToInt(parentSpriteRenderer.sprite.rect.width);
        totalImageHeight = Mathf.RoundToInt(parentSpriteRenderer.sprite.rect.height);
        pixelsPerUnit = parentSpriteRenderer.sprite.pixelsPerUnit;

        // --- 2. CÁLCULO DO TAMANHO DAS CÉLULAS ---
        cellWidthPixels = Mathf.Max(1, totalImageWidth / gridColumns);
        cellHeightPixels = Mathf.Max(1, totalImageHeight / gridRows);

        GenerateSeedPoints();
        DividePlatformIntoPixels();

        // Esconde a plataforma original após a divisão
        parentSpriteRenderer.enabled = false;
    }

    private void GenerateSeedPoints()
    {
        voronoiSeedPoints = new Vector2Int[gridColumns, gridRows];

        for (int col = 0; col < gridColumns; col++)
        {
            for (int row = 0; row < gridRows; row++)
            {
                // Gera um ponto aleatório dentro de cada célula da grelha
                int randomX = (col * cellWidthPixels) + Random.Range(0, cellWidthPixels);
                int randomY = (row * cellHeightPixels) + Random.Range(0, cellHeightPixels);
                voronoiSeedPoints[col, row] = new Vector2Int(randomX, randomY);
            }
        }
    }

    private void DividePlatformIntoPixels()
    {
        // Agrupa os píxeis por peça (ID da semente -> Lista de Píxeis)
        Dictionary<Vector2Int, List<Vector2Int>> pixelsGroupedByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();

        for (int x = 0; x < totalImageWidth; x++)
        {
            for (int y = 0; y < totalImageHeight; y++)
            {
                int currentGridX = Mathf.Clamp(x / cellWidthPixels, 0, gridColumns - 1);
                int currentGridY = Mathf.Clamp(y / cellHeightPixels, 0, gridRows - 1);

                float shortestDistance = Mathf.Infinity;
                Vector2Int closestSeedIndex = new Vector2Int();

                // Verifica a semente da célula atual e das 8 vizinhas (Regra dos 9 vizinhos)
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighborX = currentGridX + offsetX;
                        int neighborY = currentGridY + offsetY;

                        if (neighborX < 0 || neighborY < 0 || neighborX >= gridColumns || neighborY >= gridRows)
                            continue;

                        float distance = Vector2.Distance(new Vector2(x, y), (Vector2)voronoiSeedPoints[neighborX, neighborY]);

                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            closestSeedIndex = new Vector2Int(neighborX, neighborY);
                        }
                    }
                }

                if (!pixelsGroupedByPiece.ContainsKey(closestSeedIndex))
                    pixelsGroupedByPiece[closestSeedIndex] = new List<Vector2Int>();

                pixelsGroupedByPiece[closestSeedIndex].Add(new Vector2Int(x, y));
            }
        }

        CreatePieceGameObjects(pixelsGroupedByPiece);
    }

    private void CreatePieceGameObjects(Dictionary<Vector2Int, List<Vector2Int>> pixelsGroupedByPiece)
    {
        // Dados da Sprite original para mapeamento UV correto
        float spriteSheetX = parentSpriteRenderer.sprite.rect.x;
        float spriteSheetY = parentSpriteRenderer.sprite.rect.y;
        float textureFullWidth = sourceTexture.width;
        float textureFullHeight = sourceTexture.height;

        // Calcula a percentagem do Pivot (ex: 0.5 para Centro)
        Vector2 pivotOffsetPercent = new Vector2(
            parentSpriteRenderer.sprite.pivot.x / parentSpriteRenderer.sprite.rect.width,
            parentSpriteRenderer.sprite.pivot.y / parentSpriteRenderer.sprite.rect.height
        );

        foreach (var pieceEntry in pixelsGroupedByPiece)
        {
            List<Vector2Int> piecePixels = pieceEntry.Value;
            if (piecePixels.Count == 0) continue;

            // Define os limites (Bounding Box) da peça individual
            int minX = totalImageWidth, maxX = 0, minY = totalImageHeight, maxY = 0;
            foreach (Vector2Int pixel in piecePixels)
            {
                if (pixel.x < minX) minX = pixel.x; if (pixel.x > maxX) maxX = pixel.x;
                if (pixel.y < minY) minY = pixel.y; if (pixel.y > maxY) maxY = pixel.y;
            }

            int pieceWidth = maxX - minX + 1;
            int pieceHeight = maxY - minY + 1;

            Texture2D pieceTexture = new Texture2D(pieceWidth, pieceHeight);
            pieceTexture.filterMode = sourceTexture.filterMode;
            pieceTexture.wrapMode = TextureWrapMode.Clamp;

            // --- GHOST BACKGROUND (Evita fendas e outlines) ---
            for (int x = 0; x < pieceWidth; x++)
            {
                for (int y = 0; y < pieceHeight; y++)
                {
                    float u = (spriteSheetX + minX + x) / textureFullWidth;
                    float v = (spriteSheetY + minY + y) / textureFullHeight;
                    Color color = sourceTexture.GetPixelBilinear(u, v);
                    color.a = 0f; // Fundo invisível mas com a cor correta
                    pieceTexture.SetPixel(x, y, color);
                }
            }

            // --- PINTURA DOS PÍXEIS VISÍVEIS DA PEÇA ---
            foreach (Vector2Int pixel in piecePixels)
            {
                float u = (spriteSheetX + pixel.x) / textureFullWidth;
                float v = (spriteSheetY + pixel.y) / textureFullHeight;
                pieceTexture.SetPixel(pixel.x - minX, pixel.y - minY, sourceTexture.GetPixelBilinear(u, v));
            }
            pieceTexture.Apply();

            // Criação do Sprite da peça
            Sprite pieceSprite = Sprite.Create(pieceTexture, new Rect(0, 0, pieceWidth, pieceHeight), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);

            // Setup do GameObject da peça
            GameObject pieceObject = new GameObject($"PlatformPiece_{pieceEntry.Key.x}_{pieceEntry.Key.y}");
            pieceObject.transform.SetParent(this.transform);
            pieceObject.layer = LayerMask.NameToLayer("PlatformPiece");

            // --- CÁLCULO DE POSICIONAMENTO LOCAL ---
            float pieceCenterX = minX + (pieceWidth / 2f);
            float pieceCenterY = minY + (pieceHeight / 2f);

            float localPosX = (pieceCenterX - (pivotOffsetPercent.x * totalImageWidth)) / pixelsPerUnit;
            float localPosY = (pieceCenterY - (pivotOffsetPercent.y * totalImageHeight)) / pixelsPerUnit;

            pieceObject.transform.localPosition = new Vector3(localPosX, localPosY, 0);

            // Adiciona Componentes
            SpriteRenderer pieceRenderer = pieceObject.AddComponent<SpriteRenderer>();
            pieceRenderer.sprite = pieceSprite;

            pieceObject.AddComponent<PolygonCollider2D>();
            pieceObject.AddComponent<PieceCleanUp>();
        }
    }

    private void OnValidate()
    {
        if (gridColumns <= 0) gridColumns = 1;
        if (gridRows <= 0) gridRows = 1;
    }
}