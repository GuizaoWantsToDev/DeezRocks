using UnityEngine;

// Controls the aim arrow that orbits the player
public class AimArrow : MonoBehaviour
{
    [Header("=== ARROW SETTINGS ===")]
    // Distance from the player center
    [SerializeField] private float arrowRadius = 1.5f;

    // Reference to the RockThrow script
    private RockThrow rockThrow;
    private SpriteRenderer arrowRenderer;

    private void Start()
    {
        // Get the rock throw script from the parent (the player)
        rockThrow = GetComponentInParent<RockThrow>();

        // Guarda o SpriteRenderer logo no início para poupar performance
        arrowRenderer = GetComponent<SpriteRenderer>();

        // IMPORTANT TRICK: Detach the arrow from the player.
        // This prevents the arrow from flipping backwards when the player turns around!
        transform.SetParent(null);
    }

    private void Update()
    {
        // If the player dies/is destroyed, destroy the arrow too
        if (rockThrow == null)
        {
            Destroy(gameObject);
            return;
        }

        // MAGIA NOVA: Liga a seta normal apenas se a Shotgun NÃO estiver ativa!
        arrowRenderer.enabled = !rockThrow.isShotgunActive;

        // 1. Position: Stay around the player based on the aim direction and radius
        transform.position = rockThrow.transform.position + (Vector3)(rockThrow.aimDirection * arrowRadius);

        // 2. Rotation: Always point the right side of the sprite towards the aim direction
        transform.right = rockThrow.aimDirection;
    }

    // Draws a yellow circle in the Scene View to help visualize the radius
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // Find the center (parent if in editor, self if detached during gameplay)
        Vector3 centerPosition = transform.parent != null ? transform.parent.position : transform.position;

        Gizmos.DrawWireSphere(centerPosition, arrowRadius);
    }
}