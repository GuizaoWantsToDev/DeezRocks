using UnityEngine;

public class Dummie : MonoBehaviour, IDamageable
{
    private Rigidbody2D rb;
    private Vector2 startPosition;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("=== DUMMY SETTINGS ===")]
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float knockbackRecoveryTime = 0.5f;

    private bool isKnockedBack = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (isKnockedBack) return;

        // MoveTowards never overshoots — stops exactly at the target
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, startPosition, returnSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);

        // Kill all velocity so physics doesn't fight the movement
        rb.linearVelocity = Vector2.zero;
    }

    public void ReceiveKnockback()
    {
        isKnockedBack = true;
        CancelInvoke(nameof(ResetKnockback));
        Invoke(nameof(ResetKnockback), knockbackRecoveryTime);
    }

    private void ResetKnockback()
    {
        isKnockedBack = false;
        // Kill leftover velocity so it doesn't keep drifting after recovery
        rb.linearVelocity = Vector2.zero;
    }

    public void Damage(float amount)
    {
        Debug.Log($"<color=orange>DUMMY TOOK {amount} DAMAGE!</color>");

        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitColor;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.15f);
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}