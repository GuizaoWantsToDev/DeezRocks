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

        FixBrokenPieces();
    }

    private void FixBrokenPieces()
    {
        if (polygonCollider != null && polygonCollider.points.Length == 5)
        {
            Destroy(polygonCollider);

            float searchRadius = 0.5f; 
            Collider2D[] nearbyPieces = Physics2D.OverlapCircleAll(transform.position, searchRadius);

            foreach (Collider2D neighbor in nearbyPieces)
            {
                if (neighbor.gameObject != gameObject && neighbor.gameObject.layer == gameObject.layer)
                {
                    transform.SetParent(neighbor.transform);

                    break;
                }
            }
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