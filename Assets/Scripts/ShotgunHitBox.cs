using System.Collections.Generic;
using UnityEngine;

public class ShotgunHitbox : MonoBehaviour
{
    [Header("=== SETTINGS ===")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float knockbackForce = 12f;

    private GameObject ownerRoot;
    public BoxCollider2D attackCollider;

    // Objects already hit this swing — cleared every new activation
    private List<GameObject> alreadyHitThisSwing = new List<GameObject>();

    private void Awake()
    {
        attackCollider = GetComponent<BoxCollider2D>();
        attackCollider.enabled = false;
    }

    public void Initialize(GameObject owner)
    {
        ownerRoot = owner;
    }

    public void ActivateHitbox()
    {
        alreadyHitThisSwing.Clear();
        attackCollider.enabled = true;
    }

    public void DeactivateHitbox()
    {
        attackCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject hitRoot = other.transform.root.gameObject;

        if (hitRoot == ownerRoot) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("PlatformPiece")) return;

        // Only hit each object once per swing
        if (alreadyHitThisSwing.Contains(hitRoot)) return;
        alreadyHitThisSwing.Add(hitRoot);

        IDamageable damageable = hitRoot.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.Damage(damage);

        Vector2 knockbackDirection = ((Vector2)hitRoot.transform.position - (Vector2)ownerRoot.transform.position).normalized;

        PlayerController hitPlayer = hitRoot.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            hitPlayer.transform.rotation = Quaternion.Euler(0, 0, 90);
            Rigidbody2D playerRigidBody = hitPlayer.myRigidBody2D;
            playerRigidBody.linearVelocityX = 0f;
            playerRigidBody.linearVelocityY = 0f;
            hitPlayer.myRigidBody2D.AddForce(knockbackDirection * (knockbackForce + 20), ForceMode2D.Impulse);
            hitPlayer.isKnockBacked = true;
            hitPlayer.Invoke(nameof(hitPlayer.CancelKnockBack), hitPlayer.knockBackTime);
            return;
        }

        Rigidbody2D otherRigidbody = hitRoot.GetComponent<Rigidbody2D>();
        if (otherRigidbody != null)
        {
            Dummie dummie = hitRoot.GetComponent<Dummie>();
            if (dummie != null)
                dummie.ReceiveKnockback();

            otherRigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Force);

        }
    }
}