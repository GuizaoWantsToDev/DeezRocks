using System.Collections.Generic;
using UnityEngine;

public class PropSurfaceDetector : MonoBehaviour
{
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float checkRadius = 0.5f;
    [SerializeField] private Vector2 offset = new Vector2(0f, -0.5f);

    private List<GameObject> supportingPieces = new List<GameObject>();

    private void Start()
    {
        Vector2 checkPos = (Vector2)transform.position + offset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRadius, platformLayer);

        foreach (Collider2D hit in hits)
        {
            supportingPieces.Add(hit.gameObject);
        }

        if (supportingPieces.Count == 0)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        foreach (GameObject piece in supportingPieces)
        {
            if (piece == null || !piece.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere((Vector2)transform.position + offset, checkRadius);
    }
}