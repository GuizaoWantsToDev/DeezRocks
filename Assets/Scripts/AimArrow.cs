using UnityEngine;

// Controls the visual arrow that orbits the player while aiming
public class AimArrow : MonoBehaviour
{
    [Header("=== ARROW SETTINGS ===")]
    // Distance from the player's center
    [SerializeField] private float arrowRadius = 1.5f;

    private RockThrow rockThrow;
    private SpriteRenderer arrowRenderer;

    private void Start()
    {
        // Cache references to avoid calling GetComponent repeatedly
        rockThrow = GetComponentInParent<RockThrow>();
        arrowRenderer = GetComponent<SpriteRenderer>();

        // Detach from parent to prevent the arrow from flipping when the player turns around
        transform.SetParent(null);
    }

    private void Update()
    {
        // Self-destruct if the player (owner) is destroyed
        if (rockThrow == null)
        {
            Destroy(gameObject);
            return;
        }

        // Hide the standard arrow if the Shotgun mode (cone indicator) is active
        arrowRenderer.enabled = !rockThrow.isShotgunActive;

        // Calculate orbiting position based on the player's center and aiming direction
        transform.position = rockThrow.transform.position + (Vector3)(rockThrow.aimDirection * arrowRadius);

        // Align the arrow's right vector (tip) to the aiming direction
        transform.right = rockThrow.aimDirection;
    }

    // Draws a debug circle in the Scene view to visualize the orbit radius
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 centerPosition = transform.parent != null ? transform.parent.position : transform.position;
        Gizmos.DrawWireSphere(centerPosition, arrowRadius);
    }
}