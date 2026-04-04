using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Rock : MonoBehaviour
{
    [Header("General Rock Stats")]
    [SerializeField] private ParticleSystem chargeParticles;
    [SerializeField] public float baseEnergyCost;
    [SerializeField] private float baseDamage;
    [SerializeField] private float baseSpeed;
    [SerializeField] private float baseGravity;
    [SerializeField] private float extraCostPerLevel;
    [SerializeField] private float extraDamagePerLevel;
    [SerializeField] private float rockTimer;
    [SerializeField] private Sprite[] rockStage;
    [SerializeField] private LayerMask whatDestroysRock;
    private float currentRockDamage;
    private float currentRockSpeed;
    private int currentRockStage = 0;

    [Header("Other")]
    [SerializeField] private GameObject debris;
    private RockThrow rockThrow;
    private Rigidbody2D rockRigidBody2D;
    private CircleCollider2D rockCollider;
    private SpriteRenderer rockSpriteRenderer;
    private Coroutine statsCoroutine;

    private void Start()
    {
        rockRigidBody2D = GetComponent<Rigidbody2D>();
        rockCollider = GetComponent<CircleCollider2D>();
        rockSpriteRenderer = GetComponent<SpriteRenderer>();

        EnergyManager.Instance.UseEnergy(baseEnergyCost);

        currentRockDamage = baseDamage;
        rockRigidBody2D.gravityScale = 0;
        rockRigidBody2D.linearVelocity = Vector2.zero;

        statsCoroutine = StartCoroutine(InitializeRockStats());
    }

    public void SetThrowReference(RockThrow reference) 
    {
        rockThrow = reference;
        if (chargeParticles != null) chargeParticles.Play();


    }
    private IEnumerator InitializeRockStats()
    {
        while (rockThrow != null && rockThrow.inThrowState)
        {
            yield return new WaitForSeconds(rockTimer);

            if (EnergyManager.Instance.currentEnergy >= extraCostPerLevel && currentRockStage < rockStage.Length - 1)
            {
                EnergyManager.Instance.UseEnergy(extraCostPerLevel);
                currentRockStage++;
                rockSpriteRenderer.sprite = rockStage[currentRockStage];
                UpdateColliderSize();
            }
            else if (EnergyManager.Instance.currentEnergy < extraCostPerLevel)
            {
                break;
            }
        }
    }

    private void UpdateColliderSize()
    {
        Vector3 spriteHalfSize = rockSpriteRenderer.sprite.bounds.extents;
        rockCollider.radius = spriteHalfSize.x > spriteHalfSize.y ? spriteHalfSize.x : spriteHalfSize.y;
    }

    public void ReleaseRock(Vector2 shootDirection)
    {
        if (chargeParticles != null) chargeParticles.Stop();
        if (statsCoroutine != null)
            StopCoroutine(statsCoroutine);

        rockRigidBody2D.gravityScale = baseGravity;
        rockRigidBody2D.linearVelocity = shootDirection * baseSpeed;

        rockThrow = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);

        //will check if the collision is within the layermask
        if ((whatDestroysRock.value & (1 << collision.gameObject.layer)) > 0)
        {
            //add particles, SFX, scrrenshake, damage, destroy the rock

            IDamageable iDamageable = collision.gameObject.GetComponent<IDamageable>();
            if (iDamageable != null)
            {
                iDamageable.Damage(currentRockDamage);
            }

            Collider2D[] pieces = Physics2D.OverlapCircleAll(contact.point,rockCollider.radius);

            foreach (Collider2D piece in pieces)
            {
                if (piece.gameObject.layer == LayerMask.NameToLayer("PlatformPiece") || piece.gameObject.layer == LayerMask.NameToLayer("PlatformTop"))
                {
                    piece.gameObject.SetActive(false);
                    Instantiate(debris, piece.transform.position, transform.rotation);
                }
            }

            Destroy(gameObject); 
        } 
    }
    private void OnDestroy()
    {
        if (rockThrow != null && rockThrow.inThrowState)
        {
            rockThrow.ResetThrowState();
            EnergyManager.Instance.StartPassiveRegen();
        }
    }
}
