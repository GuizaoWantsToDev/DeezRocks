using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PlatformPrefabMaker
{
    [MenuItem("Prefabs/Prefab The Selected Platform")]
    public static void BakeSelectedPlatform()
    {
        GameObject selectedPlatform = Selection.activeGameObject;

        if (selectedPlatform == null)
        {
            Debug.LogWarning("ERROR: No platform selected.");
            return;
        }

        PlatformGridGenerator platform = selectedPlatform.GetComponent<PlatformGridGenerator>();

        if (platform == null)
        {
            Debug.LogWarning($"ERROR: The selected platform '{selectedPlatform.name}' doesn't have the script PlatformGridGenerator");
            return;
        }

        platform.platformSpriterenderer = platform.GetComponent<SpriteRenderer>();
        platform.platformCollider = platform.GetComponent<PolygonCollider2D>();

        MakePlatformPrefab(platform);
    }

    private static void MakePlatformPrefab(PlatformGridGenerator platform)
    {
        string platformName = platform.gameObject.name;
        string baseRootPath = "Assets/Prefabs/Platforms";

        string basePiecesPath = $"{baseRootPath}/Pieces";

        Texture2D sourceTexture = platform.platformSpriterenderer.sprite.texture;
        int imageWidth = Mathf.RoundToInt(platform.platformSpriterenderer.sprite.rect.width);
        int imageHeight = Mathf.RoundToInt(platform.platformSpriterenderer.sprite.rect.height);
        float ppu = platform.platformSpriterenderer.sprite.pixelsPerUnit;
        float sheetX = platform.platformSpriterenderer.sprite.rect.x;
        float sheetY = platform.platformSpriterenderer.sprite.rect.y;

        int cellW = Mathf.Max(1, imageWidth / platform.gridColumns);
        int cellH = Mathf.Max(1, imageHeight / platform.gridRows);

        Vector2Int[,] seedPoints = new Vector2Int[platform.gridColumns, platform.gridRows];
        float halfW = cellW / 2f; float halfH = cellH / 2f;
        for (int c = 0; c < platform.gridColumns; c++)
        {
            float offY = (c % 2 != 0) ? halfH : 0f;
            for (int r = 0; r < platform.gridRows; r++)
            {
                float cX = (c * cellW) + halfW; float cY = (r * cellH) + offY + halfH;
                int rX = Mathf.Clamp(Mathf.RoundToInt(cX + Random.Range(-halfW, halfW)), 0, imageWidth - 1);
                int rY = Mathf.Clamp(Mathf.RoundToInt(cY + Random.Range(-halfH, halfH)), 0, imageHeight - 1);
                seedPoints[c, r] = new Vector2Int(rX, rY);
            }
        }

        var pixelsByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();
        Vector2 pivot = platform.platformSpriterenderer.sprite.pivot;

        EditorUtility.DisplayProgressBar("Baking Plataforma", "A cortar píxeis...", 0.1f);

        for (int x = 0; x < imageWidth; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                Vector3 localPos = new Vector3((x - pivot.x) / ppu, (y - pivot.y) / ppu, 0);
                Vector3 worldPos = platform.transform.TransformPoint(localPos);
                if (!platform.platformCollider.OverlapPoint(worldPos)) continue;

                int gridX = Mathf.Clamp(x / cellW, 0, platform.gridColumns - 1);
                int gridY = Mathf.Clamp(y / cellH, 0, platform.gridRows - 1);
                float shortestDist = Mathf.Infinity; Vector2Int closestSeed = seedPoints[0, 0];

                for (int oX = -2; oX <= 2; oX++)
                {
                    for (int oY = -2; oY <= 2; oY++)
                    {
                        int nX = gridX + oX; int nY = gridY + oY;
                        if (nX < 0 || nY < 0 || nX >= platform.gridColumns || nY >= platform.gridRows) continue;
                        float dist = Vector2.Distance(new Vector2(x, y), seedPoints[nX, nY]);
                        if (dist < shortestDist) { shortestDist = dist; closestSeed = seedPoints[nX, nY]; }
                    }
                }
                if (!pixelsByPiece.ContainsKey(closestSeed)) pixelsByPiece[closestSeed] = new List<Vector2Int>();
                pixelsByPiece[closestSeed].Add(new Vector2Int(x, y));
            }
        }

        int targetLayer = platform.gameObject.layer;
        GameObject finalRoot = new GameObject($"{platformName}_Baked");
        int totalPieces = pixelsByPiece.Count; int currentPiece = 0;

        foreach (var piece in pixelsByPiece)
        {
            currentPiece++;
            EditorUtility.DisplayProgressBar("Baking Plataforma", $"A processar peça {currentPiece}/{totalPieces}...", 0.3f + ((float)currentPiece / totalPieces * 0.6f));

            List<Vector2Int> pixels = piece.Value;
            if (pixels.Count < 20) continue;

            int minX = imageWidth, maxX = 0, minY = imageHeight, maxY = 0;
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

            string pieceName = $"{platformName}_P_{piece.Key.x}_{piece.Key.y}";
            string relativePath = $"{basePiecesPath}/{pieceName}.png";
            byte[] bytes = pTex.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + relativePath.Substring(6), bytes);
            AssetDatabase.ImportAsset(relativePath);

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite finalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);

            GameObject pObj = new GameObject(pieceName);
            pObj.transform.SetParent(finalRoot.transform);
            pObj.layer = targetLayer;
            pObj.transform.localScale = Vector3.one;

            float centerX = minX + (pW / 2f); float centerY = minY + (pH / 2f);
            pObj.transform.localPosition = new Vector3((centerX - pivot.x) / ppu, (centerY - pivot.y) / ppu, 0);

            SpriteRenderer renderer = pObj.AddComponent<SpriteRenderer>();
            renderer.sprite = finalSprite;
            renderer.sortingLayerID = platform.platformSpriterenderer.sortingLayerID;
            renderer.sortingOrder = platform.platformSpriterenderer.sortingOrder;

            PolygonCollider2D groundPoly = pObj.AddComponent<PolygonCollider2D>();
            groundPoly.compositeOperation = Collider2D.CompositeOperation.Merge;

            PolygonCollider2D targetPoly = pObj.AddComponent<PolygonCollider2D>();
            targetPoly.compositeOperation = Collider2D.CompositeOperation.None;
            targetPoly.isTrigger = true;
        }

        Rigidbody2D rb = finalRoot.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D comp = finalRoot.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Outlines;
        comp.generationType = CompositeCollider2D.GenerationType.Synchronous;

        EditorUtility.DisplayProgressBar("Baking Plataforma", "A criar Prefab final...", 0.9f);

        string prefabPath = $"{baseRootPath}/{platformName}_Prefab.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(finalRoot, prefabPath, InteractionMode.AutomatedAction);

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Object.DestroyImmediate(finalRoot);

        Debug.Log($"<color=green>✅ Platform prefab '{platformName}' was a sucess</color> Stored in: {prefabPath}");
    }
}