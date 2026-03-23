using UnityEngine;

public class PieceCleanUp : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polygonCollider;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();

        transform.localScale = new Vector3(1.05f, 1.05f, 1f);

        if (IsCompletelyTransparent())
        {
            Destroy(gameObject);
            return;
        }

        FixBrokenColliders();
    }

    private void FixBrokenColliders()
    {
        if (polygonCollider != null && polygonCollider.points.Length == 5)
        {
            Destroy(polygonCollider);
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.3f, 0.3f);
        }
    }

    private bool IsCompletelyTransparent()
    {
        Color32[] pixels = spriteRenderer.sprite.texture.GetPixels32();

        foreach (Color32 pixel in pixels)
        {
            if (pixel.a > 5) return false;
        }

        return true;
    }
}