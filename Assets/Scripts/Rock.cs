using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : MonoBehaviour
{
    [Header("=== ROCK STATS ===")]
    [SerializeField] private ParticleSystem chargeParticles;
    [SerializeField] public float baseEnergyCost;
    [SerializeField] public float baseDamage;
    [SerializeField] private float baseSpeed;
    [SerializeField] private float baseGravity;
    [SerializeField] private float extraCostPerLevel;
    [SerializeField] private float extraDamagePerLevel;

    [Header("=== LEVEL TIMING (EXPONENTIAL) ===")]
    [SerializeField] private float baseRockTimer = 0.5f;
    [SerializeField] private float levelTimerGrowthRate = 2f;

    [Header("=== LEVEL VISUALS ===")]
    [SerializeField] private Sprite[] rockStage;
    [SerializeField] private LayerMask whatDestroysRock;

    [Header("=== DESTRUCTION SETTINGS ===")]
    [SerializeField] private float destructionRadiusMultiplier = 1.5f;

    [Header("=== OVERCHARGE (AFTER MAX LEVEL) ===")]
    [SerializeField] private float overchargeDelay = 1f;
    [SerializeField] private float overchargeDrainPerSecond = 5f;

    [Header("=== KNOCKBACK ON PLAYER HIT ===")]
    [SerializeField] private float baseKnockbackForce = 5f;
    [SerializeField] private float knockbackBonusPerLevel = 1.5f;

    [Header("=== SHOTGUN SPECIFICS ===")]
    [Tooltip("Tempo (em segundos) que as pedras da shotgun duram no ar para simular curto alcance")]
    [SerializeField] private float shotgunLifespan = 0.25f;
    [Tooltip("Multiplicador do empurrão para a Shotgun ser um ataque Anti-Rush")]
    [SerializeField] private float shotgunKnockbackMultiplier = 4f;
    [Tooltip("Quanto dano é retirado a cada bala da Shotgun (Ex: 30 tira 30 de dano)")]
    [SerializeField] private float shotgunDamagePenalty = 30f;

    [Header("=== PREVIEW SETTINGS ===")]
    [SerializeField] private float maxPreviewRange = 8f;
    [SerializeField] private float previewGrowSpeed = 6f;
    [SerializeField] private int previewStepCount = 30;
    [SerializeField] private float previewTimeStep = 0.05f;
    [SerializeField] private LayerMask previewBlockLayer;
    [SerializeField] private LineRenderer trajectoryLine;

    [Header("=== FOLLOW SETTINGS ===")]
    [SerializeField] private float orbitSmoothTime = 0.08f;

    [Header("=== OTHER ===")]
    [SerializeField] private GameObject debris;
    [SerializeField] private GameObject shockWaveManager;

    public int currentRockStage { get; private set; } = 0;
    public float currentRockDamage { get; private set; }
    public float spawnTime { get; private set; }
    public bool isShotgunRock = false;

    private float currentPreviewRange = 0f;

    private RockThrow rockThrow;
    private PlayerEnergy ownerEnergy;
    private GameObject ownerObject;
    private Transform handTransform;
    private Collider2D[] ownerColliders;

    private Rigidbody2D rockRigidBody2D;
    private CircleCollider2D rockCollider;
    private SpriteRenderer rockSpriteRenderer;
    private Coroutine levelUpCoroutine;
    private Vector2 orbitVelocity = Vector2.zero;

    private static List<Rock> allActiveRocks = new List<Rock>();

    private void OnEnable()
    {
        allActiveRocks.Add(this);
    }

    private void OnDisable()
    {
        allActiveRocks.Remove(this);
    }

    private void Awake()
    {
        rockRigidBody2D = GetComponent<Rigidbody2D>();
        rockCollider = GetComponent<CircleCollider2D>();
        rockSpriteRenderer = GetComponent<SpriteRenderer>();

        currentRockDamage = baseDamage;

        rockRigidBody2D.bodyType = RigidbodyType2D.Kinematic;
        rockRigidBody2D.gravityScale = 0f;
        rockRigidBody2D.linearVelocity = Vector2.zero;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private void Start()
    {
        if (!isShotgunRock)
        {
            levelUpCoroutine = StartCoroutine(RockLevelUpCoroutine());
        }
    }

    public void SetOwner(RockThrow throwReference, PlayerEnergy energyReference, Transform handPoint)
    {
        rockThrow = throwReference;
        ownerEnergy = energyReference;
        ownerObject = throwReference.gameObject;
        handTransform = handPoint;
        spawnTime = Time.time;

        // REDUÇÃO DE DANO DA SHOTGUN APLICADA AQUI
        if (isShotgunRock)
        {
            currentRockDamage -= shotgunDamagePenalty;
            if (currentRockDamage < 0) currentRockDamage = 0f; // Garante que o dano não fica negativo
        }

        ownerColliders = ownerObject.GetComponentsInChildren<Collider2D>();
        IgnoreOwnerAndSiblingCollisions();

        if (chargeParticles != null)
            chargeParticles.Play();
    }

    private void IgnoreOwnerAndSiblingCollisions()
    {
        if (ownerColliders != null && rockCollider != null)
        {
            foreach (Collider2D ownerCollider in ownerColliders)
            {
                if (ownerCollider != null)
                    Physics2D.IgnoreCollision(rockCollider, ownerCollider, true);
            }
        }

        foreach (Rock r in allActiveRocks)
        {
            if (r != this && IsSibling(r))
            {
                if (r.rockCollider != null && this.rockCollider != null)
                {
                    Physics2D.IgnoreCollision(this.rockCollider, r.rockCollider, true);
                }
            }
        }
    }

    public bool IsSibling(Rock otherRock)
    {
        if (otherRock == null || this.ownerObject == null || otherRock.ownerObject == null) return false;
        return this.ownerObject == otherRock.ownerObject && Mathf.Abs(this.spawnTime - otherRock.spawnTime) < 0.1f;
    }

    private void Update()
    {
        if (rockThrow != null && rockThrow.inThrowState)
        {
            CheckWallCollisionWhileHeld();
            UpdatePreview();
        }
    }

    private void FixedUpdate()
    {
        if (rockThrow == null || !rockThrow.inThrowState || handTransform == null) return;
        UpdateOrbitPosition();
    }

    private void CheckWallCollisionWhileHeld()
    {
        float checkRadius = rockCollider.radius + 0.1f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius, whatDestroysRock);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == ownerObject) continue;

            Rock otherRock = hit.GetComponent<Rock>();
            if (otherRock != null)
            {
                if (IsSibling(otherRock)) continue;

                Destroy(otherRock.gameObject);
                Explode(transform.position);
                return;
            }

            Explode(transform.position);
            return;
        }
    }

    private void UpdateOrbitPosition()
    {
        Vector2 targetPosition = handTransform.position;

        Vector2 smoothedPosition = Vector2.SmoothDamp(
            transform.position,
            targetPosition,
            ref orbitVelocity,
            orbitSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        rockRigidBody2D.MovePosition(smoothedPosition);
    }

    private void UpdatePreview()
    {
        currentPreviewRange = Mathf.MoveTowards(currentPreviewRange, maxPreviewRange, previewGrowSpeed * Time.deltaTime);

        if (trajectoryLine == null) return;

        trajectoryLine.enabled = true;
        List<Vector3> previewPoints = new List<Vector3>();

        Vector2 simulatedPosition = transform.position;
        Vector2 simulatedVelocity = rockThrow.aimDirection * baseSpeed;
        Vector2 gravityVector = Vector2.down * baseGravity * Mathf.Abs(Physics2D.gravity.y);

        float traveledDistance = 0f;
        previewPoints.Add(simulatedPosition);

        for (int step = 0; step < previewStepCount; step++)
        {
            float distanceThisStep = simulatedVelocity.magnitude * previewTimeStep;
            if (traveledDistance + distanceThisStep > currentPreviewRange) break;

            Vector2 nextPosition = simulatedPosition + simulatedVelocity * previewTimeStep;
            simulatedVelocity += gravityVector * previewTimeStep;

            RaycastHit2D wallHit = Physics2D.Linecast(simulatedPosition, nextPosition, previewBlockLayer);
            if (wallHit.collider != null)
            {
                previewPoints.Add(wallHit.point);
                break;
            }

            simulatedPosition = nextPosition;
            traveledDistance += distanceThisStep;
            previewPoints.Add(simulatedPosition);
        }

        trajectoryLine.positionCount = previewPoints.Count;
        trajectoryLine.SetPositions(previewPoints.ToArray());
    }

    private IEnumerator RockLevelUpCoroutine()
    {
        while (rockThrow != null && rockThrow.inThrowState)
        {
            float waitTime = baseRockTimer * Mathf.Pow(levelTimerGrowthRate, currentRockStage);
            yield return new WaitForSeconds(waitTime);

            if (rockThrow == null || !rockThrow.inThrowState) break;

            bool canAffordUpgrade = ownerEnergy.HasEnough(extraCostPerLevel);
            bool hasNextStage = currentRockStage < rockStage.Length - 1;

            if (canAffordUpgrade && hasNextStage)
            {
                ownerEnergy.UseEnergy(extraCostPerLevel);
                currentRockStage++;
                currentRockDamage += extraDamagePerLevel;
                rockSpriteRenderer.sprite = rockStage[currentRockStage];
                UpdateColliderSize();

                bool reachedMaxLevel = currentRockStage >= rockStage.Length - 1;
                if (reachedMaxLevel)
                {
                    StartCoroutine(OverchargeDrainCoroutine());
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    private IEnumerator OverchargeDrainCoroutine()
    {
        yield return new WaitForSeconds(overchargeDelay);

        while (rockThrow != null && rockThrow.inThrowState)
        {
            ownerEnergy.UseEnergy(overchargeDrainPerSecond * Time.deltaTime);
            yield return null;
        }
    }

    private void UpdateColliderSize()
    {
        Vector3 spriteHalfSize = rockSpriteRenderer.sprite.bounds.extents;
        rockCollider.radius = Mathf.Max(spriteHalfSize.x, spriteHalfSize.y);
        IgnoreOwnerAndSiblingCollisions();
    }

    public void ReleaseRock(Vector2 shootDirection)
    {
        if (chargeParticles != null) chargeParticles.gameObject.SetActive(false);
        if (levelUpCoroutine != null) StopCoroutine(levelUpCoroutine);
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        rockRigidBody2D.bodyType = RigidbodyType2D.Dynamic;
        rockRigidBody2D.gravityScale = baseGravity;
        rockRigidBody2D.linearVelocity = shootDirection * baseSpeed;

        rockThrow = null;

        if (isShotgunRock)
        {
            Destroy(gameObject, shotgunLifespan);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rock otherRock = collision.gameObject.GetComponent<Rock>();
        if (otherRock != null)
        {
            if (IsSibling(otherRock))
            {
                Physics2D.IgnoreCollision(this.rockCollider, collision.collider, true);
                return;
            }

            Destroy(collision.gameObject);
            Explode(transform.position);
            return;
        }

        bool hitDestructibleLayer = (whatDestroysRock.value & (1 << collision.gameObject.layer)) > 0;
        bool hitOwner = collision.gameObject == ownerObject;

        if (!hitDestructibleLayer || hitOwner) return;

        ContactPoint2D contact = collision.GetContact(0);

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.Damage(currentRockDamage);

        PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
            float scaledKnockbackForce = baseKnockbackForce + (knockbackBonusPerLevel * currentRockStage);

            if (isShotgunRock)
            {
                scaledKnockbackForce *= shotgunKnockbackMultiplier;
            }

            hitPlayer.myRigidBody2D.AddForce(knockbackDirection * scaledKnockbackForce, ForceMode2D.Impulse);
            hitPlayer.isKnockBacked = true;
            hitPlayer.Invoke(nameof(hitPlayer.CancelKnockBack), hitPlayer.knockBackTime);
        }

        Explode(contact.point);
    }

    private void Explode(Vector2 hitPoint)
    {
        bool isHighEnoughLevelForShockwave = currentRockStage >= 3;
        if (isHighEnoughLevelForShockwave && shockWaveManager != null)
            Instantiate(shockWaveManager, hitPoint, transform.rotation);

        DestroyPlatformPieces(hitPoint);

        if (rockThrow != null && rockThrow.inThrowState)
        {
            rockThrow.ResetThrowState();
            if (ownerEnergy != null) ownerEnergy.StartPassiveRegen();
        }

        Destroy(gameObject);
    }

    private void DestroyPlatformPieces(Vector2 hitPoint)
    {
        int platformPieceMask = LayerMask.GetMask("PlatformPiece");
        ContactFilter2D platformFilter = new ContactFilter2D();
        platformFilter.SetLayerMask(platformPieceMask);
        platformFilter.useTriggers = true;

        List<Collider2D> hitPieces = new List<Collider2D>();
        float destructionRadius = rockCollider.radius * destructionRadiusMultiplier;

        Physics2D.OverlapCircle(hitPoint, destructionRadius, platformFilter, hitPieces);

        foreach (Collider2D piece in hitPieces)
        {
            piece.gameObject.SetActive(false);
            if (debris != null)
                Instantiate(debris, piece.transform.position, transform.rotation);
        }
    }

    private void OnDestroy()
    {
        if (rockThrow != null && rockThrow.inThrowState)
        {
            rockThrow.ResetThrowState();
            if (ownerEnergy != null) ownerEnergy.StartPassiveRegen();
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayRockHit();
    }
}