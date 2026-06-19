using UnityEngine;

public class ShotgunHitbox : MonoBehaviour
{
    private PolygonCollider2D shotgunCollider;
    private PlayerController myPlayer;

    private float shotgunDamage;
    private float shotgunKnockbackForce;

    private void Start()
    {
        myPlayer = GetComponentInParent<PlayerController>();
        shotgunCollider = GetComponent<PolygonCollider2D>();

        shotgunCollider.enabled = false;

        shotgunDamage = MobilityAndCombatStats.Instance.shotgunDamage;
        shotgunKnockbackForce = MobilityAndCombatStats.Instance.shotgunKnockbackForce;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == myPlayer.gameObject)
        {
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("PlatformPiece"))
        {
            return;
        }

        if (other.TryGetComponent<PlayerController>(out PlayerController playerHit))
        {
            RockThrow hitRockThrow = playerHit.GetComponent<RockThrow>();

            if (hitRockThrow != null && hitRockThrow.inThrowState)
            {
                hitRockThrow.ForceRelease();
            }

            playerHit.StartKnockedStage();
            playerHit.myRigidBody2D.linearVelocity = Vector3.zero;
            playerHit.myRigidBody2D.AddForce(transform.right * shotgunKnockbackForce, ForceMode2D.Impulse);
        }

        if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.Damage(shotgunDamage);
        }

        if (other.TryGetComponent<Dummie>(out Dummie dummie))
        {
            dummie.StartKnockedStage();
            dummie.myRigidBody2D.AddForce(transform.right * shotgunKnockbackForce, ForceMode2D.Impulse);
        }
    }
}