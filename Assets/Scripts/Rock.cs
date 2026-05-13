using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls the behavior, physics, collisions, and leveling of the projectiles
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

    [Header("=== MASS PER LEVEL ===")]
    [SerializeField] private float[] massPerStage = { 1f, 2f, 4f, 7f, 12f };

    [Header("=== DESTRUCTION SETTINGS ===")]
    [SerializeField] private float destructionRadiusMultiplier = 1.5f;

    [Header("=== OVERCHARGE & KNOCKBACK ===")]
    [SerializeField] private float overchargeDelay = 1f;
    [SerializeField] private float overchargeDrainPerSecond = 5f;
    [SerializeField] private float baseKnockbackForce = 5f;
    [SerializeField] private float knockbackBonusPerLevel = 1.5f;

    [Header("=== SELF DAMAGE SETTINGS ===")]
    [Tooltip("Time before the rock can hit the owner after being thrown")]
    [SerializeField] private float selfDamageDelay = 0.15f;
    private bool canHurtOwner = false;

    [Header("=== PREVIEW & FOLLOW SETTINGS ===")]
    [SerializeField] private float maxPreviewRange = 8f;
    [SerializeField] private float previewGrowSpeed = 6f;
    [SerializeField] private int previewStepCount = 30;
    [SerializeField] private float previewTimeStep = 0.05f;
    [SerializeField] private LayerMask previewBlockLayer;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private float orbitSmoothTime = 0.08f;

    [Header("=== OTHER ===")]
    [SerializeField] private GameObject debris;
    [SerializeField] private GameObject shockWaveManager;

    public int currentRockStage { get; private set; } = 0;
    public float currentRockDamage { get; private set; }
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

    private void Awake()
    {
        rockRigidBody2D = GetComponent<Rigidbody2D>();
        rockCollider = GetComponent<CircleCollider2D>();
        rockSpriteRenderer = GetComponent<SpriteRenderer>();

        currentRockDamage = baseDamage;

        // SENIOR MAGIC: Spawn as Dynamic so it can smash into walls while held!
        // We set gravity to 0 so it floats in the hand until thrown.
        rockRigidBody2D.bodyType = RigidbodyType2D.Dynamic;
        rockRigidBody2D.gravityScale = 0f;
        rockRigidBody2D.linearVelocity = Vector2.zero;

        ApplyMassForCurrentStage();

        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    private void Start()
    {
        levelUpCoroutine = StartCoroutine(RockLevelUpCoroutine());
    }

    // Called by RockThrow right after spawning — sets up all owner references
    public void SetOwner(RockThrow throwReference, PlayerEnergy energyReference, Transform handPoint)
    {
        rockThrow = throwReference;
        ownerEnergy = energyReference;
        ownerObject = throwReference.gameObject;
        handTransform = handPoint;
        canHurtOwner = false;

        ownerColliders = ownerObject.GetComponentsInChildren<Collider2D>();
        ToggleOwnerCollisions(true); // Ignore collisions with the player holding it

        if (chargeParticles != null) chargeParticles.Play();
    }

    // Turns collisions with the owner ON or OFF
    private void ToggleOwnerCollisions(bool ignore)
    {
        if (ownerColliders == null || rockCollider == null) return;

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
                Physics2D.IgnoreCollision(rockCollider, ownerCollider, ignore);
        }
    }

    // Sets the Rigidbody mass to match the current rock stage
    private void ApplyMassForCurrentStage()
    {
        if (massPerStage == null || massPerStage.Length == 0) return;
        int stageIndex = Mathf.Clamp(currentRockStage, 0, massPerStage.Length - 1);
        rockRigidBody2D.mass = massPerStage[stageIndex];
    }

    private void Update()
    {
        if (rockThrow == null || !rockThrow.inThrowState) return;
        UpdatePreview();
    }

    private void FixedUpdate()
    {
        if (rockThrow == null || !rockThrow.inThrowState || handTransform == null) return;
        UpdateOrbitPosition();
    }

    // Smoothly follows the hand/throwDirection using SmoothDamp (works perfectly with Dynamic bodies)
    private void UpdateOrbitPosition()
    {
        Vector2 targetPosition = handTransform.position;
        Vector2 smoothedPosition = Vector2.SmoothDamp(transform.position, targetPosition, ref orbitVelocity, orbitSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        rockRigidBody2D.MovePosition(smoothedPosition);
    }

    // Simulates and draws the predicted flight path
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

    // Coroutine to incrementally level up the rock's damage, mass, and visual state
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

                ApplyMassForCurrentStage();
                UpdateColliderSize();

                if (currentRockStage >= rockStage.Length - 1)
                {
                    StartCoroutine(OverchargeDrainCoroutine());
                    break;
                }
            }
            else break;
        }
    }

    // Slowly drains the player's energy if they hold the rock at maximum level
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
        ToggleOwnerCollisions(true); // Re-apply ignore just in case size change resets it
    }

    // Detaches the rock from the player and applies shooting physics
    public void ReleaseRock(Vector2 shootDirection)
    {
        if (chargeParticles != null) chargeParticles.gameObject.SetActive(false);
        if (levelUpCoroutine != null) StopCoroutine(levelUpCoroutine);
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        // Restore gravity now that it's in the air
        rockRigidBody2D.gravityScale = baseGravity;
        rockRigidBody2D.linearVelocity = shootDirection * baseSpeed;

        rockThrow = null;

        // Start delay to enable self-damage (boomerang effect)
        StartCoroutine(EnableSelfDamageCoroutine());
    }

    // Waits a tiny bit before letting the rock hit the player who threw it
    private IEnumerator EnableSelfDamageCoroutine()
    {
        yield return new WaitForSeconds(selfDamageDelay);
        canHurtOwner = true;
        ToggleOwnerCollisions(false); // Turn collisions back ON!
    }

    // Handles ALL impact logic: even if the player smashes the rock into the ground while holding it!
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // --- ROCK VS ROCK ---
        Rock otherRock = collision.gameObject.GetComponent<Rock>();
        if (otherRock != null)
        {
            bool thisIsMaxLevel = currentRockStage >= rockStage.Length - 1;
            bool otherIsMinLevel = otherRock.currentRockStage == 0;

            if (thisIsMaxLevel && otherIsMinLevel)
            {
                Destroy(otherRock.gameObject);
                Explode(transform.position);
            }
            return;
        }

        // --- ROCK VS WORLD & PLAYERS ---
        bool hitDestructibleLayer = (whatDestroysRock.value & (1 << collision.gameObject.layer)) > 0;
        bool hitOwner = collision.gameObject == ownerObject;

        // If we hit ourselves before the self-damage delay is over, ignore it.
        if (hitOwner && !canHurtOwner) return;

        // Only process hits on destructible layers or players
        PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (!hitDestructibleLayer && hitPlayer == null) return;

        ContactPoint2D contact = collision.GetContact(0);

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.Damage(currentRockDamage);

        if (hitPlayer != null)
        {
            Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
            float scaledKnockbackForce = baseKnockbackForce + (knockbackBonusPerLevel * currentRockStage);
            hitPlayer.myRigidBody2D.AddForce(knockbackDirection * scaledKnockbackForce, ForceMode2D.Impulse);
            hitPlayer.isKnockBacked = true;
            hitPlayer.Invoke(nameof(hitPlayer.CancelKnockBack), hitPlayer.knockBackTime);
        }

        Explode(contact.point);
    }

    // Executes explosion visuals and triggers terrain breaking
    private void Explode(Vector2 hitPoint)
    {
        bool isHighEnoughLevelForShockwave = currentRockStage >= 3;
        if (isHighEnoughLevelForShockwave && shockWaveManager != null)
            Instantiate(shockWaveManager, hitPoint, transform.rotation);

        DestroyPlatformPieces(hitPoint);


        Destroy(gameObject);
    }

    // Disables platform prefabs overlapping the explosion radius and spawns collectible debris
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

    // Handles cleanup if the rock is destroyed while the player is charging it
    private void OnDestroy()
    {
        if (rockThrow != null && rockThrow.inThrowState)
        {
            rockThrow.ResetThrowState(); // Safely reset the player's throwing animation
            if (ownerEnergy != null) ownerEnergy.StartPassiveRegen();
        }
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayRockHit();
    }
}