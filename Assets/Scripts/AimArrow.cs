using UnityEngine;

public class AimArrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private float arrowRadius = 1.5f;

    private RockThrow rockThrow;
    private SpriteRenderer arrowRenderer;

    private void Start()
    {
        rockThrow = GetComponentInParent<RockThrow>();
        arrowRenderer = GetComponent<SpriteRenderer>();

        transform.SetParent(null);
    }

    private void Update()
    {
        if (rockThrow == null)
        {
            Destroy(gameObject);
            return;
        }

        arrowRenderer.enabled = !rockThrow.isShotgunActive;

        transform.position = rockThrow.transform.position + (Vector3)(rockThrow.aimDirection * arrowRadius);
        transform.right = rockThrow.aimDirection;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 centerPosition = transform.position;

        if (transform.parent != null)
        {
            centerPosition = transform.parent.position;
        }

        Gizmos.DrawWireSphere(centerPosition, arrowRadius);
    }
}