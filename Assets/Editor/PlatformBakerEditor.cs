#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(PlatformGenerator))]
public class PlatformBakerEditor : Editor
{
    private PlatformGenerator baker;
    private string basePiecesPath;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Desenha as variáveis normais

        baker = (PlatformGenerator)target;

        GUILayout.Space(20);
        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("?? BAKE PLATFORM TO PREFAB ??", GUILayout.Height(40)))
        {
            PrepareFolders();
            ExecuteBake();
        }
        GUI.backgroundColor = Color.white;
    }

    private void PrepareFolders()
    {
        // Cria a pasta principal: Assets/BakedPlatforms/NomeDaPlataforma
        string rootPath = "Assets/BakedPlatforms";
        if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "BakedPlatforms");

        string platformPath = $"{rootPath}/{baker.folderName}";
        if (!AssetDatabase.IsValidFolder(platformPath)) AssetDatabase.CreateFolder(rootPath, baker.folderName);

        // Cria subpasta para as Texturas (PNGs)
        basePiecesPath = $"{platformPath}/Pieces";
        if (!AssetDatabase.IsValidFolder(basePiecesPath)) AssetDatabase.CreateFolder(platformPath, "Pieces");
    }

    private void ExecuteBake()
    {
        if (baker.spriteRenderer.sprite == null) { Debug.LogError("Falta a Sprite original!"); return; }

        // --- 1. SETTINGS INICIAIS ---
        Texture2D sourceTexture = baker.spriteRenderer.sprite.texture;
        int imgW = Mathf.RoundToInt(baker.spriteRenderer.sprite.rect.width);
        int imgH = Mathf.RoundToInt(baker.spriteRenderer.sprite.rect.height);
        float ppu = baker.spriteRenderer.sprite.pixelsPerUnit;
        float sheetX = baker.spriteRenderer.sprite.rect.x;
        float sheetY = baker.spriteRenderer.sprite.rect.y;

        int cellW = Mathf.Max(1, imgW / baker.gridColumns);
        int cellH = Mathf.Max(1, imgH / baker.gridRows);

        // --- 2. GERAR PONTOS VORONOI (Caos Máximo) ---
        Vector2Int[,] seedPoints = new Vector2Int[baker.gridColumns, baker.gridRows];
        float halfW = cellW / 2f; float halfH = cellH / 2f;
        for (int c = 0; c < baker.gridColumns; c++)
        {
            float offY = (c % 2 != 0) ? halfH : 0f;
            for (int r = 0; r < baker.gridRows; r++)
            {
                float cX = (c * cellW) + halfW; float cY = (r * cellH) + offY + halfH;
                int rX = Mathf.Clamp(Mathf.RoundToInt(cX + Random.Range(-halfW, halfW)), 0, imgW - 1);
                int rY = Mathf.Clamp(Mathf.RoundToInt(cY + Random.Range(-halfH, halfH)), 0, imgH - 1);
                seedPoints[c, r] = new Vector2Int(rX, rY);
            }
        }

        // --- 3. DIVIDIR PÍXEIS (Corta-bolachas) ---
        var pixelsByPiece = new Dictionary<Vector2Int, List<Vector2Int>>();
        Vector2 pivot = baker.spriteRenderer.sprite.pivot;

        EditorUtility.DisplayProgressBar("Baking Plataforma", "A cortar píxeis...", 0.1f);

        for (int x = 0; x < imgW; x++)
        {
            for (int y = 0; y < imgH; y++)
            {
                // Verificação de limites usando o collider original
                Vector3 localPos = new Vector3((x - pivot.x) / ppu, (y - pivot.y) / ppu, 0);
                Vector3 worldPos = baker.transform.TransformPoint(localPos);
                if (!baker.mainCollider.OverlapPoint(worldPos)) continue;

                int gridX = Mathf.Clamp(x / cellW, 0, baker.gridColumns - 1);
                int gridY = Mathf.Clamp(y / cellH, 0, baker.gridRows - 1);
                float shortestDist = Mathf.Infinity; Vector2Int closestSeed = seedPoints[0, 0];

                for (int oX = -2; oX <= 2; oX++)
                {
                    for (int oY = -2; oY <= 2; oY++)
                    {
                        int nX = gridX + oX; int nY = gridY + oY;
                        if (nX < 0 || nY < 0 || nX >= baker.gridColumns || nY >= baker.gridRows) continue;
                        float dist = Vector2.Distance(new Vector2(x, y), seedPoints[nX, nY]);
                        if (dist < shortestDist) { shortestDist = dist; closestSeed = seedPoints[nX, nY]; }
                    }
                }
                if (!pixelsByPiece.ContainsKey(closestSeed)) pixelsByPiece[closestSeed] = new List<Vector2Int>();
                pixelsByPiece[closestSeed].Add(new Vector2Int(x, y));
            }
        }

        // --- 4. CRIAR OBJETOS E GRAVAR TEXTURAS ---
        int targetLayer = LayerMask.NameToLayer(baker.pieceLayer);
        GameObject finalRoot = new GameObject($"{baker.folderName}_Baked");
        int totalPieces = pixelsByPiece.Count; int currentPiece = 0;

        foreach (var piece in pixelsByPiece)
        {
            currentPiece++;
            EditorUtility.DisplayProgressBar("Baking Plataforma", $"A processar peça {currentPiece}/{totalPieces}...", 0.3f + ((float)currentPiece / totalPieces * 0.6f));

            List<Vector2Int> pixels = piece.Value;
            if (pixels.Count < 20) continue; // Ignora pedaços minúsculos

            // Calcular limites da textura da peça
            int minX = imgW, maxX = 0, minY = imgH, maxY = 0;
            foreach (Vector2Int p in pixels)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
            int pW = maxX - minX + 1; int pH = maxY - minY + 1;

            // Criar textura e preencher
            Texture2D pTex = new Texture2D(pW, pH, TextureFormat.RGBA32, false);
            pTex.filterMode = FilterMode.Bilinear; // Suave
            Color transparent = new Color(0, 0, 0, 0);
            for (int i = 0; i < pTex.width; i++) for (int j = 0; j < pTex.height; j++) pTex.SetPixel(i, j, transparent);
            foreach (Vector2Int p in pixels)
            {
                int sX = Mathf.RoundToInt(sheetX) + p.x; int sY = Mathf.RoundToInt(sheetY) + p.y;
                pTex.SetPixel(p.x - minX, p.y - minY, sourceTexture.GetPixel(sX, sY));
            }
            pTex.Apply();

            // GRAVAR O FICHEIRO PNG NO DISCO!
            string pieceName = $"P_{piece.Key.x}_{piece.Key.y}";
            string relativePath = $"{basePiecesPath}/{pieceName}.png";
            byte[] bytes = pTex.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + relativePath.Substring(6), bytes); // Salva fisicamente
            AssetDatabase.ImportAsset(relativePath); // Diz ao Unity para importar

            // CONFIGURAR AS DEFINIÇÕES DA TEXTURA IMPORTADA (Mudar para Sprite, Bilinear, etc)
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f); // Centro
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // Qualidade máxima
            importer.SaveAndReimport();

            // Carregar a Sprite recém-criada como Asset
            Sprite finalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);

            // Criar o GameObject na Hierarquia
            GameObject pObj = new GameObject(pieceName);
            pObj.transform.SetParent(finalRoot.transform);
            pObj.layer = targetLayer;
            pObj.transform.localScale = Vector3.one;

            // Posicionamento correto
            float centerX = minX + (pW / 2f); float centerY = minY + (pH / 2f);
            pObj.transform.localPosition = new Vector3((centerX - pivot.x) / ppu, (centerY - pivot.y) / ppu, 0);

            // Adicionar Renderer e Material
            SpriteRenderer renderer = pObj.AddComponent<SpriteRenderer>();
            renderer.sprite = finalSprite;
            renderer.sortingLayerID = baker.spriteRenderer.sortingLayerID;
            renderer.sortingOrder = baker.spriteRenderer.sortingOrder;
            if (baker.pieceMaterial != null) renderer.material = baker.pieceMaterial;

            // Adicionar Colisor (para as peças terem física se caírem!)
            pObj.AddComponent<PolygonCollider2D>();
        }

        // --- 5. GUARDAR COMO PREFAB FINAL ---
        EditorUtility.DisplayProgressBar("Baking Plataforma", "A criar Prefab final...", 0.9f);
        string prefabPath = $"{basePiecesPath.Replace("/Pieces", "")}/{baker.folderName}_Prefab.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(finalRoot, prefabPath, InteractionMode.AutomatedAction);

        // Limpeza
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>? PLATAFORMA '{baker.folderName}' BAKADA COM SUCESSO!</color> Gravada em: {prefabPath}");
    }
}
#endif