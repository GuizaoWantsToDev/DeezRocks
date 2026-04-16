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
    [SerializeField] private float rockTimer;
    [SerializeField] private Sprite[] rockStage;
    [SerializeField] private LayerMask whatDestroysRock;

    [Header("=== FOLLOW SETTINGS ===")]
    [SerializeField] private float orbitSmoothTime = 0.08f;

    [Header("=== PREVIEW SETTINGS ===")]
    [SerializeField] private float maxPreviewRange = 8f;
    [SerializeField] private float previewGrowSpeed = 6f;
    [SerializeField] private int previewStepCount = 30;
    [SerializeField] private float previewTimeStep = 0.05f;
    [SerializeField] private LayerMask previewBlockLayer;
    [SerializeField] private LineRenderer trajectoryLine;

    [Header("=== OTHER ===")]
    [SerializeField] private GameObject debris;
    [SerializeField] private GameObject shockWaveManager;

    public float currentRockDamage { get; private set; }
    private int currentRockStage = 0;
    private float currentPreviewRange = 0f;

    private RockThrow rockThrow;
    private PlayerEnergy ownerEnergy;
    private GameObject ownerObject;
    private Transform handTransform; // <-- Esta é a mão que vamos seguir
    private Collider2D[] ownerColliders;

    private Rigidbody2D rockRigidBody2D;
    private CircleCollider2D rockCollider;
    private SpriteRenderer rockSpriteRenderer;
    private Coroutine statsCoroutine;

    private Vector2 orbitVelocity = Vector2.zero;

    private void Awake()
    {
        rockRigidBody2D = GetComponent<Rigidbody2D>();
        rockCollider = GetComponent<CircleCollider2D>();
        rockSpriteRenderer = GetComponent<SpriteRenderer>();

        currentRockDamage = baseDamage;
        rockRigidBody2D.gravityScale = 0f;
        rockRigidBody2D.linearVelocity = Vector2.zero;

        // Dynamic para bater nas paredes se o player a arrastar
        rockRigidBody2D.bodyType = RigidbodyType2D.Dynamic;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private void Start()
    {
        statsCoroutine = StartCoroutine(RockLevelUpCoroutine());
    }

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
        if (rockThrow == null || !rockThrow.inThrowState) return;

        UpdatePreview();
    }

    private void FixedUpdate()
    {
        if (rockThrow == null || !rockThrow.inThrowState || handTransform == null) return;

        UpdateOrbitPosition();
    }

    private void UpdateOrbitPosition()
    {
        // Aqui está o Vector2.SmoothDamp para o throwDirection!
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
        currentPreviewRange = Mathf.MoveTowards(
            currentPreviewRange,
            maxPreviewRange,
            previewGrowSpeed * Time.deltaTime
        );

        if (trajectoryLine == null) return;

        trajectoryLine.enabled = true;

        List<Vector3> previewPoints = new List<Vector3>();
        Vector2 simulatedPosition = transform.position;
        Vector2 simulatedVelocity = rockThrow.aimDirection * baseSpeed;
        Vector2 gravityVector = Vector2.down * baseGravity * Physics2D.gravity.magnitude;

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
            yield return new WaitForSeconds(rockTimer);

            if (ownerEnergy.HasEnough(extraCostPerLevel) && currentRockStage < rockStage.Length - 1)
            {
                ownerEnergy.UseEnergy(extraCostPerLevel);
                currentRockStage++;
                currentRockDamage += extraDamagePerLevel;
                rockSpriteRenderer.sprite = rockStage[currentRockStage];
                UpdateColliderSize();
            }
            else if (!ownerEnergy.HasEnough(extraCostPerLevel))
            {
                break;
            }
        }
    }

    private void UpdateColliderSize()
    {
        Vector3 spriteHalfSize = rockSpriteRenderer.sprite.bounds.extents;
        rockCollider.radius = Mathf.Max(spriteHalfSize.x, spriteHalfSize.y);
        IgnoreOwnerCollisions();
    }

    public void ReleaseRock(Vector2 shootDirection)
    {
        if (chargeParticles != null)
            chargeParticles.gameObject.SetActive(false);

        if (statsCoroutine != null)
            StopCoroutine(statsCoroutine);

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        rockRigidBody2D.gravityScale = baseGravity;
        rockRigidBody2D.linearVelocity = shootDirection * baseSpeed;

        rockThrow = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((whatDestroysRock.value & (1 << collision.gameObject.layer)) > 0 && collision.gameObject != ownerObject)
        {
            ContactPoint2D contact = collision.GetContact(0);

            if (shockWaveManager != null)
                Instantiate(shockWaveManager, contact.point, transform.rotation);

            IDamageable iDamageable = collision.gameObject.GetComponent<IDamageable>();
            if (iDamageable != null)
                iDamageable.Damage(currentRockDamage);

            int platformPieceLayer = LayerMask.GetMask("PlatformPiece");
            ContactFilter2D platformFilter = new ContactFilter2D();
            platformFilter.SetLayerMask(platformPieceLayer);
            platformFilter.useTriggers = true;

            List<Collider2D> hitPieces = new List<Collider2D>();
            Physics2D.OverlapCircle(contact.point, rockCollider.radius + 0.5f, platformFilter, hitPieces);

            foreach (Collider2D piece in hitPieces)
            {
                piece.gameObject.SetActive(false);
                if (debris != null)
                    Instantiate(debris, piece.transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (rockThrow != null && rockThrow.inThrowState)
        {
            rockThrow.ResetThrowState();

            if (ownerEnergy != null)
                ownerEnergy.StartPassiveRegen();
        }
    }
}