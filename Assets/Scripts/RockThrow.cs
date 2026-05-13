using UnityEngine;
using UnityEngine.InputSystem;

// Manages both weapons: the chargeable rock and the shotgun melee attack
public class RockThrow : MonoBehaviour
{
    private PlayerController player;
    private PlayerInput playerInput;
    private PlayerEnergy playerEnergy;

    [Header("=== AIMING ===")]
    public Vector2 aimDirection = Vector2.right;
    private Vector2 rawAimInput;

    [Header("=== WEAPON STATE ===")]
    public bool inThrowState;
    public bool isShotgunActive = false;
    public WeaponUIManager weaponUI;

    [Header("=== SHOTGUN ===")]
    [SerializeField] private ShotgunHitbox shotgunHitbox;
    [SerializeField] private float shotgunActiveDuration = 0.3f;
    [SerializeField] private float shotgunEnergyCost = 20f;
    [SerializeField] private SpriteRenderer shotgunConeIndicator;

    [Header("=== ROCK ===")]
    [SerializeField] private Rock rock;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject throwPoint;
    [SerializeField] private Transform throwDirection;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private float baseRecoilForce = 1f;
    [SerializeField] private float recoilGrowthRate = 1.8f;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float spawnCheckRadius = 0.3f;

    [Header("=== VISUALS ===")]
    [SerializeField] private SpriteRenderer armRenderer;

    [Header("=== COOLDOWN ===")]
    [SerializeField] private float attackCooldown = 1f;
    private float nextAttackTime;

    private GameObject spawnedRock;
    public float holdTime { get; private set; }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        playerEnergy = GetComponent<PlayerEnergy>();

        if (shotgunHitbox != null)
            shotgunHitbox.Initialize(gameObject);

        if (shotgunConeIndicator != null)
            shotgunConeIndicator.enabled = false;
    }

    private void Update()
    {
        HandleAimDirection();
        player.myAnimator.SetBool("ThrowState", inThrowState);

        // Track how long the player has been holding the rock
        if (inThrowState && !isShotgunActive)
        {
            holdTime += Time.deltaTime;
            if (spawnedRock != null)
                spawnedRock.transform.rotation = throwPoint.transform.rotation;
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
        if (!context.performed || inThrowState) return;

        isShotgunActive = !isShotgunActive;

        if (weaponUI != null) weaponUI.UpdateWeaponUI(isShotgunActive);
        if (shotgunConeIndicator != null) shotgunConeIndicator.enabled = isShotgunActive;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    private void HandleAimDirection()
    {
        if (playerInput.currentControlScheme == "Keyboard")
        {
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                aimDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
            }
        }
        else
        {
            if (rawAimInput.magnitude > 0.25f)
                aimDirection = rawAimInput.normalized;
        }

        throwPoint.transform.right = aimDirection;
        armRenderer.flipY = aimDirection.x < 0;

        if (shotgunConeIndicator != null)
            shotgunConeIndicator.flipY = armRenderer.flipY;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState && Time.time >= nextAttackTime)
        {
            if (isShotgunActive)
                FireShotgun();
            else
                StartChargingRock();
        }

        if (context.canceled && inThrowState && !isShotgunActive)
            ReleaseRock();
    }

    private void FireShotgun()
    {
        if (!playerEnergy.HasEnough(shotgunEnergyCost) || player.isWalled) return;

        playerEnergy.UseEnergy(shotgunEnergyCost);
        player.myAnimator.SetTrigger("ShotgunAttack");

        if (shotgunHitbox != null)
            shotgunHitbox.ActivateHitbox();

        // Deactivate the hitbox after the attack window ends
        Invoke(nameof(DeactivateShotgun), shotgunActiveDuration);

        armRenderer.enabled = true;
        Invoke(nameof(HideArm), 0.15f);

        nextAttackTime = Time.time + attackCooldown;
    }

    private void DeactivateShotgun()
    {
        if (shotgunHitbox != null)
            shotgunHitbox.DeactivateHitbox();
    }

    private void StartChargingRock()
    {
        if (!playerEnergy.HasEnough(rock.baseEnergyCost) || player.isWalled) return;

        // Don't allow spawning inside a platform
        Vector2 spawnPosition = throwDirection.position;
        if (Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, platformLayer) != null) return;

        inThrowState = true;
        holdTime = 0f;
        player.ResetGravity();
        playerEnergy.StopPassiveRegen();

        if (trajectoryLine != null) trajectoryLine.enabled = true;

        spawnedRock = Instantiate(rockPrefab, spawnPosition, throwPoint.transform.rotation);
        if (spawnedRock != null)
        {
            Rock rockScript = spawnedRock.GetComponent<Rock>();
            playerEnergy.UseEnergy(rockScript.baseEnergyCost);
            rockScript.SetOwner(this, playerEnergy, throwDirection);
        }

        armRenderer.enabled = true;
    }

    private void ReleaseRock()
    {
        if (spawnedRock != null)
        {
            Rock rockScript = spawnedRock.GetComponent<Rock>();
            if (rockScript != null)
            {
                rockScript.ReleaseRock(aimDirection);

                // Recoil grows exponentially with rock level — level 1 is soft, level 5 is strong
                float recoilForce = baseRecoilForce * Mathf.Pow(recoilGrowthRate, rockScript.currentRockStage);
                ApplyRecoil(recoilForce);
            }
        }

        spawnedRock = null;
        nextAttackTime = Time.time + attackCooldown;
        ResetThrowState();
        playerEnergy.StartPassiveRegen();
    }

    private void ApplyRecoil(float force)
    {
        player.myRigidBody2D.linearVelocity = Vector2.zero;
        Vector2 recoilDirection = -aimDirection;

        // Small upward bump so horizontal recoil doesn't feel flat
        if (Mathf.Abs(recoilDirection.x) > 0.5f && recoilDirection.y >= -0.1f && recoilDirection.y < 0.2f)
            recoilDirection.y = 0.2f;

        player.isKnockBacked = true;
        player.myRigidBody2D.AddForce(recoilDirection.normalized * force, ForceMode2D.Impulse);
        player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);
    }

    public void ResetThrowState()
    {
        inThrowState = false;
        holdTime = 0f;
        armRenderer.enabled = false;
        player.myAnimator.SetBool("ThrowState", false);

        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (spawnedRock != null) Destroy(spawnedRock);
        spawnedRock = null;
    }

    private void HideArm()
    {
        if (!inThrowState) armRenderer.enabled = false;
    }

    public void ForceRelease()
    {
        if (inThrowState && !isShotgunActive) ReleaseRock();
    }
}