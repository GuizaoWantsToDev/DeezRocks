using UnityEngine;
using UnityEngine.InputSystem;

// Handles the combat input, aiming, and instantiation of projectiles
public class RockThrow : MonoBehaviour
{
    private PlayerController player;
    private PlayerInput playerInput;
    private PlayerEnergy playerEnergy;

    [Header("=== STATE & AIMING ===")]
    public bool inThrowState;
    public Vector2 aimDirection = Vector2.right;
    private Vector2 rawAimInput;

    [Header("=== SHOTGUN UPGRADE ===")]
    public bool isShotgunActive = false;
    public Transform[] shotgunShootPoints;
    public WeaponUIManager weaponUI;
    [SerializeField] private float shotgunEnergyMultiplier = 3f;

    [Header("=== VISUALS & EFFECTS ===")]
    [SerializeField] private SpriteRenderer armRender;
    [SerializeField] private ParticleSystem particleSystems;
    [SerializeField] private SpriteRenderer shotgunConeIndicator;

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

        // Ensure the cone visual reflects the starting weapon state
        if (shotgunConeIndicator != null) shotgunConeIndicator.enabled = isShotgunActive;
    }

    private void Update()
    {
        HandleAimDirection();
        player.myAnimator.SetBool("ThrowState", inThrowState);

        // Keep updating the instantiated rock's rotation while charging the normal attack
        if (inThrowState && !isShotgunActive)
        {
            holdTime += Time.deltaTime;

            if (rockInst != null)
            {
                rockInst.transform.rotation = throwPoint.transform.rotation;
            }
        }
        else
        {
            holdTime = 0f;
        }
    }

    // Reads raw aiming vector from the new Input System
    public void OnAim(InputAction.CallbackContext context)
    {
        rawAimInput = context.ReadValue<Vector2>();
    }

    // Toggles between Normal Rock and Shotgun fire modes
    public void OnSwapWeapon(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState)
        {
            isShotgunActive = !isShotgunActive;
            if (weaponUI != null) weaponUI.UpdateWeaponUI(isShotgunActive);

            // Toggle visual indicators based on the active weapon
            if (shotgunConeIndicator != null) shotgunConeIndicator.enabled = isShotgunActive;
            if (trajectoryLine != null) trajectoryLine.enabled = !isShotgunActive && inThrowState;
        }
    }

    // Calculates the normalized aiming direction using the mouse or controller analog stick
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
        armRender.flipY = aimDirection.x < 0; // Flip the arm sprite correctly

        if (shotgunConeIndicator != null) shotgunConeIndicator.flipY = armRender.flipY;
    }

    // Handles the primary attack button press. Starts charging a rock or fires the shotgun instantly.
    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState && Time.time >= nextThrowTime)
        {
            float baseCost = rock.baseEnergyCost;

            if (isShotgunActive)
            {
                float totalShotgunCost = baseCost * shotgunEnergyMultiplier;

                if (playerEnergy.HasEnough(totalShotgunCost) && !player.isWalled)
                {
                    // Validation: Prevent spawning rocks inside walls
                    bool spawnInsidePlatform = false;
                    foreach (Transform sPoint in shotgunShootPoints)
                    {
                        if (sPoint != null && Physics2D.OverlapCircle(sPoint.position, spawnCheckRadius, platformLayer) != null)
                        {
                            spawnInsidePlatform = true;
                            break;
                        }
                    }
                    if (spawnInsidePlatform) return;

                    playerEnergy.UseEnergy(totalShotgunCost);

                    armRender.enabled = true;
                    Invoke(nameof(HideArm), 0.15f); // Briefly show arm for visual feedback

                    // Instantiate and instantly fire rocks from all defined shotgun spread points
                    for (int i = 0; i < shotgunShootPoints.Length; i++)
                    {
                        Transform sPoint = shotgunShootPoints[i];
                        if (sPoint == null) continue;

                        GameObject sgRock = Instantiate(rockPrefab, sPoint.position, sPoint.rotation);
                        Rock newRockScript = sgRock.GetComponent<Rock>();

                        if (newRockScript != null)
                        {
                            newRockScript.isShotgunRock = true;
                            newRockScript.SetOwner(this, playerEnergy, sPoint);
                            newRockScript.ReleaseRock(sPoint.right); // Fire immediately
                        }
                    }

                    // Apply recoil force specific to the shotgun burst
                    ApplyRecoil(baseRecoilForce * 2.5f);
                    nextThrowTime = Time.time + throwCooldown;
                }
            }
            else // NORMAL ROCK MODE
            {
                if (playerEnergy.HasEnough(baseCost) && !player.isWalled)
                {
                    Vector2 spawnPosition = throwDirection.position;
                    if (Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, platformLayer) != null) return;

                    inThrowState = true;
                    holdTime = 0f;
                    player.ResetGravity();
                    playerEnergy.StopPassiveRegen();

                    if (trajectoryLine != null) trajectoryLine.enabled = true;

                    // Instantiate the charging rock attached to the player
                    rockInst = Instantiate(rockPrefab, spawnPosition, throwPoint.transform.rotation);
                    if (rockInst != null)
                    {
                        Rock newRockScript = rockInst.GetComponent<Rock>();
                        playerEnergy.UseEnergy(newRockScript.baseEnergyCost);
                        newRockScript.SetOwner(this, playerEnergy, throwDirection);
                    }
                    armRender.enabled = true;
                }
            }
        }

        // Release the standard rock when the input button is released
        if (context.canceled && inThrowState && !isShotgunActive)
        {
            FireRock();
        }
    }

    // Handles the release logic for the standard chargeable rock
    private void FireRock()
    {
        if (rockInst != null)
        {
            Rock normalRock = rockInst.GetComponent<Rock>();
            if (normalRock != null)
            {
                normalRock.ReleaseRock(aimDirection);
                float recoilForce = baseRecoilForce * Mathf.Pow(recoilGrowthRate, normalRock.currentRockStage);

                // Apply calculated recoil force based on the rock's charge stage
                ApplyRecoil(recoilForce);
            }
        }
        rockInst = null;
        nextThrowTime = Time.time + throwCooldown;
        ResetThrowState();
        playerEnergy.StartPassiveRegen();
    }

    // Centralized function to calculate and apply consistent recoil knockback to the player
    private void ApplyRecoil(float forceMagnitude)
    {
        // Halt current linear momentum to ensure recoil is predictable regardless of movement state
        player.myRigidBody2D.linearVelocity = Vector2.zero;
        Vector2 recoilDir = -aimDirection;
   
        if (Mathf.Abs(recoilDir.x) > 0.5f && recoilDir.y >= -0.1f && recoilDir.y < 0.2f)
        {
            recoilDir.y = 0.2f;
        }

        player.isKnockBacked = true;
        player.myRigidBody2D.AddForce(recoilDir.normalized * forceMagnitude, ForceMode2D.Impulse);
        player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);
    }

    // Cleans up the aiming state and resets visuals
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

    // Failsafe to force release if energy runs out or player is interrupted
    public void ForceRelease()
    {
        if (inThrowState && !isShotgunActive) FireRock();
    }
}