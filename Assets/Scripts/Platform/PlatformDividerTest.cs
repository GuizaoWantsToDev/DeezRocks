using System.Collections.Generic;
using UnityEngine;


public class PlatformDividerTest : MonoBehaviour
{
    private void Start()
    {
        PlatformGridGenerator gridGenerator = GetComponent<PlatformGridGenerator>();
        SpriteRenderer platformSpriteRenderer = GetComponent<SpriteRenderer>();
        PolygonCollider2D platformCollider = GetComponent<PolygonCollider2D>();

        if (platformSpriteRenderer.sprite == null) return;

        Texture2D sourceTexture = platformSpriteRenderer.sprite.texture;
        int imgWidth = Mathf.RoundToInt(platformSpriteRenderer.sprite.rect.width);
        int imgHeight = Mathf.RoundToInt(platformSpriteRenderer.sprite.rect.height);
        float ppu = platformSpriteRenderer.sprite.pixelsPerUnit;
        float sheetX = platformSpriteRenderer.sprite.rect.x;
        float sheetY = platformSpriteRenderer.sprite.rect.y;

        int cellW = Mathf.Max(1, imgWidth / Mathf.Max(1, gridGenerator.gridColumns));
        int cellH = Mathf.Max(1, imgHeight / Mathf.Max(1, gridGenerator.gridRows));

        Vector2Int[,] seedPoints = new Vector2Int[gridGenerator.gridColumns, gridGenerator.gridRows];
        float halfW = cellW / 2f; float halfH = cellH / 2f;

        for (int c = 0; c < gridGenerator.gridColumns; c++)
        {
            float offY = (c % 2 != 0) ? halfH : 0f;
            for (int r = 0; r < gridGenerator.gridRows; r++)
            {
                float cX = (c * cellW) + halfW; float cY = (r * cellH) + offY + halfH;
                int rX = Mathf.Clamp(Mathf.RoundToInt(cX + Random.Range(-halfW, halfW)), 0, imgWidth - 1);
                int rY = Mathf.Clamp(Mathf.RoundToInt(cY + Random.Range(-halfH, halfH)), 0, imgHeight - 1);
                seedPoints[c, r] = new Vector2Int(rX, rY);
            }
        }

        var pixelsByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();
        Vector2 pivot = platformSpriteRenderer.sprite.pivot;

        for (int x = 0; x < imgWidth; x++)
        {
            for (int y = 0; y < imgHeight; y++)
            {
                Vector3 localPos = new Vector3((x - pivot.x) / ppu, (y - pivot.y) / ppu, 0);
                Vector3 worldPos = transform.TransformPoint(localPos);
                if (!platformCollider.OverlapPoint(worldPos)) continue;

                int gridX = Mathf.Clamp(x / cellW, 0, gridGenerator.gridColumns - 1);
                int gridY = Mathf.Clamp(y / cellH, 0, gridGenerator.gridRows - 1);
                float shortestDist = Mathf.Infinity; Vector2Int closestSeed = seedPoints[0, 0];

                for (int oX = -2; oX <= 2; oX++)
                {
                    for (int oY = -2; oY <= 2; oY++)
                    {
                        int nX = gridX + oX; int nY = gridY + oY;
                        if (nX < 0 || nY < 0 || nX >= gridGenerator.gridColumns || nY >= gridGenerator.gridRows) continue;
                        float dist = Vector2.Distance(new Vector2(x, y), seedPoints[nX, nY]);
                        if (dist < shortestDist) { shortestDist = dist; closestSeed = seedPoints[nX, nY]; }
                    }
                }

                if (!pixelsByPiece.ContainsKey(closestSeed)) pixelsByPiece[closestSeed] = new List<Vector2Int>();
                pixelsByPiece[closestSeed].Add(new Vector2Int(x, y));
            }
        }

        int targetLayer = gameObject.layer;

        foreach (var piece in pixelsByPiece)
        {
            List<Vector2Int> pixels = piece.Value;
            if (pixels.Count < 20) continue;

            int minX = imgWidth, maxX = 0, minY = imgHeight, maxY = 0;
            foreach (Vector2Int p in pixels)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
            int pW = maxX - minX + 1; int pH = maxY - minY + 1;

            Texture2D pTex = new Texture2D(pW, pH, TextureFormat.RGBA32, false);
            pTex.filterMode = FilterMode.Bilinear;
            Color transparent = new Color(0, 0, 0, 0);
            for (int i = 0; i < pTex.width; i++) for (int j = 0; j < pTex.height; j++) pTex.SetPixel(i, j, transparent);

            foreach (Vector2Int p in pixels)
            {
                int sX = Mathf.RoundToInt(sheetX) + p.x; int sY = Mathf.RoundToInt(sheetY) + p.y;
                pTex.SetPixel(p.x - minX, p.y - minY, sourceTexture.GetPixel(sX, sY));
            }
            pTex.Apply();

            Sprite pieceSprite = Sprite.Create(pTex, new Rect(0, 0, pW, pH), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);

            GameObject pObj = new GameObject($"Piece_{piece.Key.x}_{piece.Key.y}");
            pObj.transform.SetParent(transform);
            pObj.layer = targetLayer;
            pObj.transform.localScale = Vector3.one;

            float centerX = minX + (pW / 2f); float centerY = minY + (pH / 2f);
            pObj.transform.localPosition = new Vector3((centerX - pivot.x) / ppu, (centerY - pivot.y) / ppu, 0);

            SpriteRenderer renderer = pObj.AddComponent<SpriteRenderer>();
            renderer.sprite = pieceSprite;
            renderer.sortingLayerID = platformSpriteRenderer.sortingLayerID;
            renderer.sortingOrder = platformSpriteRenderer.sortingOrder;

            pObj.AddComponent<PolygonCollider2D>();
        }

        platformSpriteRenderer.enabled = false;
        platformCollider.enabled = false;
    }
}