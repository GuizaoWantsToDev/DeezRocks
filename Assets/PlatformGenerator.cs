using UnityEngine;

// Script que fica na cena para configurares o Bake
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class PlatformGenerator : MonoBehaviour
{
    [Header("Configurações da Grelha")]
    public int gridColumns = 10;
    public int gridRows = 4;

    [Header("Configurações de Saída")]
    public string folderName = "BakedPlatform_01";
    public string pieceLayer = "Default";
    public Material pieceMaterial; // Opcional: Se quiseres um material específico

    // Variáveis ocultas que o Editor Script vai ler
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public PolygonCollider2D mainCollider;

    private void OnValidate()
    {
        // Garante que apanhamos as referências no Editor
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<PolygonCollider2D>();
    }
}