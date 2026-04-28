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
    // Time to reach level 2. Each next level takes longer exponentially.
    [SerializeField] private float baseRockTimer = 0.5f;
    // How much longer each level takes compared to the previous one
    [SerializeField] private float levelTimerGrowthRate = 2f;

    [Header("=== LEVEL VISUALS ===")]
    [SerializeField] private Sprite[] rockStage;
    [SerializeField] private LayerMask whatDestroysRock;

    [Header("=== DESTRUCTION SETTINGS ===")]
    // Multiplier for the destruction circle. 1.5 means the explosion is 50% larger than the rock.
    // This fixes the issue where the contact point caused small holes for big rocks.
    [SerializeField] private float destructionRadiusMultiplier = 1.5f;

    [Header("=== OVERCHARGE (AFTER MAX LEVEL) ===")]
    // Seconds to wait at max level before energy starts draining
    [SerializeField] private float overchargeDelay = 1f;
    // Energy drained per second after overcharge delay
    [SerializeField] private float overchargeDrainPerSecond = 5f;

    [Header("=== KNOCKBACK ON PLAYER HIT ===")]
    // Knockback force at level 1
    [SerializeField] private float baseKnockbackForce = 5f;
    // Extra knockback added per rock level
    [SerializeField] private float knockbackBonusPerLevel = 1.5f;

    [Header("=== PREVIEW SETTINGS ===")]
    [SerializeField] private float maxPreviewRange = 8f;
    [SerializeField] private float previewGrowSpeed = 6f;
    [SerializeField] private int previewStepCount = 30;
    [SerializeField] private float previewTimeStep = 0.05f;
    [SerializeField] private LayerMask previewBlockLayer;
    [SerializeField] private LineRenderer trajectoryLine;

    [Header("=== FOLLOW SETTINGS ===")]
    // Time delay for the rock to catch up to the player's hand
    [SerializeField] private float orbitSmoothTime = 0.08f;

    [Header("=== OTHER ===")]
    [SerializeField] private GameObject debris;
    [SerializeField] private GameObject shockWaveManager;

    // Current level index (0 = level 1, 4 = level 5)
    public int currentRockStage { get; private set; } = 0;
    public float currentRockDamage { get; private set; }

    private float currentPreviewRange = 0f;

    // References
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

    private void Awake()
    {
        rockRigidBody2D = GetComponent<Rigidbody2D>();
        rockCollider = GetComponent<CircleCollider2D>();
        rockSpriteRenderer = GetComponent<SpriteRenderer>();

        currentRockDamage = baseDamage;

        // Kinematic while held so it ignores gravity and static walls.
        // We will manually check for walls in Update() while holding.
        rockRigidBody2D.bodyType = RigidbodyType2D.Kinematic;
        rockRigidBody2D.gravityScale = 0f;
        rockRigidBody2D.linearVelocity = Vector2.zero;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private void Start()
    {
        levelUpCoroutine = StartCoroutine(RockLevelUpCoroutine());
    }

    // Called by RockThrow right after spawning the rock
    public void SetOwner(RockThrow throwReference, PlayerEnergy energyReference, Transform handPoint)
    {
        rockThrow = throwReference;
        ownerEnergy = energyReference;
        ownerObject = throwReference.gameObject;
        handTransform = handPoint;

        ownerColliders = ownerObject.GetComponentsInChildren<Collider2D>();
        IgnoreOwnerCollisions();

        if (chargeParticles != null)
            chargeParticles.Play();
    }

    // Prevents the rock from colliding with the player who threw it
    private void IgnoreOwnerCollisions()
    {
        if (ownerColliders == null || rockCollider == null) return;

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
                Physics2D.IgnoreCollision(rockCollider, ownerCollider, true);
        }
    }

    private void Update()
    {
        // If the player is actively holding the rock
        if (rockThrow != null && rockThrow.inThrowState)
        {
            CheckWallCollisionWhileHeld();
            UpdatePreview();
        }
    }

    private void FixedUpdate()
    {
        // Smoothly follow the hand during physics updates
        if (rockThrow == null || !rockThrow.inThrowState || handTransform == null) return;

        UpdateOrbitPosition();
    }

    // Check if the player dragged the rock into a wall while aiming
    private void CheckWallCollisionWhileHeld()
    {
        // Added +0.1f buffer to the radius so it detects the wall BEFORE it clips inside
        float checkRadius = rockCollider.radius + 0.1f;
        Collider2D hitWall = Physics2D.OverlapCircle(transform.position, checkRadius, whatDestroysRock);

        // If we hit something that destroys the rock, and it's not the player himself
        if (hitWall != null && hitWall.gameObject != ownerObject)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayRockHit();

            DestroyPlatformPieces(transform.position);

            // Clean up the throw state on the player before destroying
            rockThrow.ResetThrowState();
            if (ownerEnergy != null) ownerEnergy.StartPassiveRegen();

            Destroy(gameObject);
        }
    }

    // Smoothly move the rock to the hand position using SmoothDamp
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

    // Draw the predicted flight path line
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

            // Stop line if it hits a wall
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

    // Levels up the rock with exponential wait times
    private IEnumerator RockLevelUpCoroutine()
    {
        while (rockThrow != null && rockThrow.inThrowState)
        {
            // Exponential wait logic
            float waitTime = baseRockTimer * Mathf.Pow(levelTimerGrowthRate, currentRockStage);
            yield return new WaitForSeconds(waitTime);

            if (rockThrow == null || !rockThrow.inThrowState) break;

            bool canAffordUpgrade = ownerEnergy.HasEnough(extraCostPerLevel);
            bool hasNextStage = currentRockStage < rockStage.Length - 1;

            if (canAffordUpgrade && hasNextStage)
            {
                // Apply upgrade
                ownerEnergy.UseEnergy(extraCostPerLevel);
                currentRockStage++;
                currentRockDamage += extraDamagePerLevel;
                rockSpriteRenderer.sprite = rockStage[currentRockStage];
                UpdateColliderSize();

                // Check if reached Max level to start Overcharge
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

    // Drains energy smoothly after holding at max level for too long
    private IEnumerator OverchargeDrainCoroutine()
    {
        yield return new WaitForSeconds(overchargeDelay);

        while (rockThrow != null && rockThrow.inThrowState)
        {
            // Drain energy incrementally every frame
            ownerEnergy.UseEnergy(overchargeDrainPerSecond * Time.deltaTime);
            yield return null;
        }
    }

    // Adjusts collider to fit new sprite
    private void UpdateColliderSize()
    {
        Vector3 spriteHalfSize = rockSpriteRenderer.sprite.bounds.extents;
        rockCollider.radius = Mathf.Max(spriteHalfSize.x, spriteHalfSize.y);
        IgnoreOwnerCollisions();
    }

    // Throws the rock
    public void ReleaseRock(Vector2 shootDirection)
    {
        if (chargeParticles != null) chargeParticles.gameObject.SetActive(false);
        if (levelUpCoroutine != null) StopCoroutine(levelUpCoroutine);
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        // Switch to Dynamic physics to fly normally
        rockRigidBody2D.bodyType = RigidbodyType2D.Dynamic;
        rockRigidBody2D.gravityScale = baseGravity;
        rockRigidBody2D.linearVelocity = shootDirection * baseSpeed;

        rockThrow = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Two rocks hit each other
        Rock otherRock = collision.gameObject.GetComponent<Rock>();
        if (otherRock != null)
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
            return;
        }

        // 2. Hits something destructible
        bool hitDestructibleLayer = (whatDestroysRock.value & (1 << collision.gameObject.layer)) > 0;
        bool hitOwner = collision.gameObject == ownerObject;

        if (!hitDestructibleLayer || hitOwner) return;

        ContactPoint2D contact = collision.GetContact(0);

        // Spawn shockwave only on levels 4 and 5 (index 3 and 4)
        bool isHighEnoughLevelForShockwave = currentRockStage >= 3;
        if (isHighEnoughLevelForShockwave && shockWaveManager != null)
            Instantiate(shockWaveManager, contact.point, transform.rotation);

        // Damage target
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.Damage(currentRockDamage);

        // Apply knockback to players
        PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
            float scaledKnockbackForce = baseKnockbackForce + (knockbackBonusPerLevel * currentRockStage);

            hitPlayer.myRigidBody2D.AddForce(knockbackDirection * scaledKnockbackForce, ForceMode2D.Impulse);
            hitPlayer.isKnockBacked = true;
            hitPlayer.Invoke(nameof(hitPlayer.CancelKnockBack), hitPlayer.knockBackTime);
        }

        // Destroy dynamic platforms using the contact point
        DestroyPlatformPieces(contact.point);

        Destroy(gameObject);
    }

    // Helper method to break platforms (used by collision and by wall-drag check)
    private void DestroyPlatformPieces(Vector2 hitPoint)
    {
        int platformPieceMask = LayerMask.GetMask("PlatformPiece");
        ContactFilter2D platformFilter = new ContactFilter2D();
        platformFilter.SetLayerMask(platformPieceMask);
        platformFilter.useTriggers = true;

        List<Collider2D> hitPieces = new List<Collider2D>();

        // FIX: Multiply the rock radius by a destruction multiplier.
        // Since we are checking from the edge (contact point), the circle needs to be larger to reach deep inside the wall.
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
        // Cleanup if destroyed while held
        if (rockThrow != null && rockThrow.inThrowState)
        {
            rockThrow.ResetThrowState();
            if (ownerEnergy != null) ownerEnergy.StartPassiveRegen();
        }
            // Play global impact sound
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayRockHit();
    }
}