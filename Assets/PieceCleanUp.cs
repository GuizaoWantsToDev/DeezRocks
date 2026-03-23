using System.Collections;
using UnityEngine;

public class PieceCleanUp : MonoBehaviour
{
    private SpriteRenderer pieceSpriteRenderer;
    private PolygonCollider2D piecePollygonCollider;

    private void Start()
    {
        pieceSpriteRenderer = GetComponent<SpriteRenderer>();
        piecePollygonCollider = GetComponent<PolygonCollider2D>();

        if (IsCompletelyTransparent())
        {
            Destroy(gameObject);   
        }

        FixBrokenColliders();
    }
    private void FixBrokenColliders()
    {
            if (piecePollygonCollider.points.Length == 5)
            {
                Destroy(piecePollygonCollider);
                BoxCollider2D colliderSwitch = gameObject.AddComponent<BoxCollider2D>();
                colliderSwitch.size = new Vector2(0.3f, 0.3f);
             }
    }
    private bool IsCompletelyTransparent()
    {
        Color32[] pixels = pieceSpriteRenderer.sprite.texture.GetPixels32();

        foreach (Color32 pixel in pixels)
        { 
            if (pixel.a > 5)
            {
                return false;
            }
        }

        return true;
    }
}