using UnityEngine;
using UnityEngine.InputSystem;

public class RockThrow : MonoBehaviour
{
    private PlayerController player;
    private PlayerInput playerInput;
    private PlayerEnergy playerEnergy;

    [Header("=== STATE & AIMING ===")]
    public bool inThrowState;
    public Vector2 aimDirection = Vector2.right;
    private Vector2 rawAimInput;

    [Header("=== SHOTGUN RAYCAST ===")]
    [SerializeField] private int shotgunRayCount = 5;
    [SerializeField] private float shotgunConeAngle = 30f;
    [SerializeField] private float shotgunRange = 8f;
    [SerializeField] private float shotgunDamagePerRay = 10f;
    [SerializeField] private float shotgunKnockbackForce = 6f;
    [SerializeField] private float shotgunLineVisibleDuration = 0.08f;
    [SerializeField] private LayerMask shotgunHitLayer;
    [SerializeField] private float shotgunEnergyMultiplier = 3f;
    [SerializeField] private GameObject shotgunLinePrefab;

    [Header("=== SHOTGUN UI ===")]
    public bool isShotgunActive = false;
    public WeaponUIManager weaponUI;
    [SerializeField] private SpriteRenderer shotgunConeIndicator;

    [Header("=== VISUALS & EFFECTS ===")]
    [SerializeField] private SpriteRenderer armRender;
    [SerializeField] private ParticleSystem particleSystems;

    [Header("=== ROCK SETUP & PREFABS ===")]
    [SerializeField] private Rock rock;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject throwPoint;
    [SerializeField] private Transform throwDirection;
    [SerializeField] private LineRenderer trajectoryLine;
    private GameObject rockInst;

    [Header("=== RECOIL (EXPONENTIAL) ===")]
    [SerializeField] private float baseRecoilForce = 1f;
    [SerializeField] private float recoilGrowthRate = 1.8f;

    [Header("=== COOLDOWN SETTINGS ===")]
    [SerializeField] private float throwCooldown = 1f;
    private float nextThrowTime;

    [Header("=== SPAWN VALIDATION ===")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float spawnCheckRadius = 0.3f;

    public float holdTime { get; private set; }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        playerEnergy = GetComponent<PlayerEnergy>();

        if (shotgunConeIndicator != null)
            shotgunConeIndicator.enabled = isShotgunActive;
    }

    private void Update()
    {
        HandleAimDirection();
        player.myAnimator.SetBool("ThrowState", inThrowState);

        if (inThrowState && !isShotgunActive)
        {
            holdTime += Time.deltaTime;

            if (rockInst != null)
                rockInst.transform.rotation = throwPoint.transform.rotation;
        }
        else
        {
            holdTime = 0f;
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        rawAimInput = context.ReadValue<Vector2>();
    }

    public void OnSwapWeapon(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState)
        {
            isShotgunActive = !isShotgunActive;

            if (weaponUI != null) weaponUI.UpdateWeaponUI(isShotgunActive);
            if (shotgunConeIndicator != null) shotgunConeIndicator.enabled = isShotgunActive;
            if (trajectoryLine != null) trajectoryLine.enabled = !isShotgunActive && inThrowState;
        }
    }

    private void HandleAimDirection()
    {
        if (playerInput.currentControlScheme == "Keyboard")
        {
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                aimDirection = (mousePosition - (Vector2)transform.position).normalized;
            }
        }
        else
        {
            if (rawAimInput.magnitude > 0.25f)
                aimDirection = rawAimInput.normalized;
        }

        throwPoint.transform.right = aimDirection;
        armRender.flipY = aimDirection.x < 0;

        if (shotgunConeIndicator != null)
            shotgunConeIndicator.flipY = armRender.flipY;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState && Time.time >= nextThrowTime)
        {
            if (isShotgunActive)
            {
                FireShotgun();
            }
            else
            {
                StartChargingRock();
            }
        }

        if (context.canceled && inThrowState && !isShotgunActive)
        {
            FireRock();
        }
    }

    private void FireShotgun()
    {
        float totalCost = rock.baseEnergyCost * shotgunEnergyMultiplier;

        if (!playerEnergy.HasEnough(totalCost) || player.isWalled) return;

        playerEnergy.UseEnergy(totalCost);

        float angleStep = shotgunRayCount > 1 ? shotgunConeAngle / (shotgunRayCount - 1) : 0f;
        float startAngle = -shotgunConeAngle / 2f;

        for (int i = 0; i < shotgunRayCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            Vector2 rayDirection = RotateVector(aimDirection, currentAngle);
            Vector2 rayOrigin = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, shotgunRange, shotgunHitLayer);

            Vector2 rayEndPoint = hit.collider != null ? hit.point : rayOrigin + rayDirection * shotgunRange;

            if (hit.collider != null)
            {
                // Never damage ourselves with our own shotgun rays!
                if (hit.collider.gameObject != gameObject)
                {
                    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                        damageable.Damage(shotgunDamagePerRay);

                    PlayerController hitPlayer = hit.collider.GetComponent<PlayerController>();
                    if (hitPlayer != null)
                    {
                        hitPlayer.myRigidBody2D.AddForce(rayDirection * shotgunKnockbackForce, ForceMode2D.Impulse);
                        hitPlayer.isKnockBacked = true;
                        hitPlayer.Invoke(nameof(hitPlayer.CancelKnockBack), hitPlayer.knockBackTime);
                    }
                }
            }

            if (shotgunLinePrefab != null)
            {
                GameObject lineObj = Instantiate(shotgunLinePrefab, rayOrigin, Quaternion.identity);
                LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();

                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, rayOrigin);
                    lineRenderer.SetPosition(1, rayEndPoint);
                }
                Destroy(lineObj, shotgunLineVisibleDuration);
            }
        }

        armRender.enabled = true;
        Invoke(nameof(HideArm), 0.15f);

        ApplyRecoil(baseRecoilForce * 2.5f);
        nextThrowTime = Time.time + throwCooldown;
    }

    private Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void StartChargingRock()
    {
        if (!playerEnergy.HasEnough(rock.baseEnergyCost) || player.isWalled) return;

        Vector2 spawnPosition = throwDirection.position;
        if (Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, platformLayer) != null) return;

        inThrowState = true;
        holdTime = 0f;
        player.ResetGravity();
        playerEnergy.StopPassiveRegen();

        if (trajectoryLine != null) trajectoryLine.enabled = true;

        rockInst = Instantiate(rockPrefab, spawnPosition, throwPoint.transform.rotation);
        if (rockInst != null)
        {
            Rock newRockScript = rockInst.GetComponent<Rock>();
            playerEnergy.UseEnergy(newRockScript.baseEnergyCost);
            newRockScript.SetOwner(this, playerEnergy, throwDirection);
        }

        armRender.enabled = true;
    }

    private void FireRock()
    {
        if (rockInst != null)
        {
            Rock normalRock = rockInst.GetComponent<Rock>();
            if (normalRock != null)
            {
                normalRock.ReleaseRock(aimDirection);
                float recoilForce = baseRecoilForce * Mathf.Pow(recoilGrowthRate, normalRock.currentRockStage);
                ApplyRecoil(recoilForce);
            }
        }

        rockInst = null;
        nextThrowTime = Time.time + throwCooldown;
        ResetThrowState();
        playerEnergy.StartPassiveRegen();
    }

    private void ApplyRecoil(float forceMagnitude)
    {
        player.myRigidBody2D.linearVelocity = Vector2.zero;
        Vector2 recoilDirection = -aimDirection;

        if (Mathf.Abs(recoilDirection.x) > 0.5f && recoilDirection.y >= -0.1f && recoilDirection.y < 0.2f)
            recoilDirection.y = 0.2f;

        player.isKnockBacked = true;
        player.myRigidBody2D.AddForce(recoilDirection.normalized * forceMagnitude, ForceMode2D.Impulse);
        player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);
    }

    public void ResetThrowState()
    {
        inThrowState = false;
        holdTime = 0f;
        armRender.enabled = false;
        player.myAnimator.SetBool("ThrowState", false);

        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (rockInst != null) Destroy(rockInst);
        rockInst = null;
    }

    private void HideArm()
    {
        if (!inThrowState) armRender.enabled = false;
    }

    public void ForceRelease()
    {
        if (inThrowState && !isShotgunActive) FireRock();
    }
}