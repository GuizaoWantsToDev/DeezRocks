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

    [Header("=== VISUALS & EFFECTS ===")]
    [SerializeField] private SpriteRenderer armRender;
    [SerializeField] private ParticleSystem particleSystems;

    [Header("=== ROCK SETUP & PREFABS ===")]
    [SerializeField] private Rock rock;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject throwPoint;
    [SerializeField] private Transform throwDirection;
    private GameObject rockInst;

    [Header("=== RECOIL (EXPONENTIAL) ===")]
    // Base recoil for level 1 rock
    [SerializeField] private float baseRecoilForce = 1f;
    // Growth multiplier for recoil per rock level
    [SerializeField] private float recoilGrowthRate = 1.8f;

    [Header("=== COOLDOWN SETTINGS ===")]
    [SerializeField] private float throwCooldown = 1f;
    private float nextThrowTime;

    [Header("=== SPAWN VALIDATION ===")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float spawnCheckRadius = 0.3f;

    // Tracks how long the button is held for PlayerController slow-fall calculation
    public float holdTime { get; private set; }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        playerEnergy = GetComponent<PlayerEnergy>();
    }

    private void Update()
    {
        HandleAimDirection();
        player.myAnimator.SetBool("ThrowState", inThrowState);

        if (inThrowState)
        {
            // Increase hold timer
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

    private void HandleAimDirection()
    {
        if (playerInput.currentControlScheme == "Keyboard")
        {
            // Aim exactly at the mouse pointer
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                aimDirection = (mousePosition - (Vector2)transform.position).normalized;
            }
        }
        else
        {
            // Controller: 25% deadzone to prevent stick snap-back bugs
            if (rawAimInput.magnitude > 0.25f)
            {
                aimDirection = rawAimInput.normalized;
            }
            else
            {
                // If stick is resting, check if current aim is inside a wall.
                // If inside wall, reset aim to face forward.
                Vector2 aimCheckPosition = (Vector2)transform.position + aimDirection * spawnCheckRadius;
                bool aimInsidePlatform = Physics2D.OverlapCircle(aimCheckPosition, 0.1f, platformLayer) != null;

                if (aimInsidePlatform)
                {
                    aimDirection = transform.right; // Reset to forward direction
                }
            }
        }

        throwPoint.transform.right = aimDirection;
        armRender.flipY = aimDirection.x < 0; // Flip arm sprite if aiming left
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState && Time.time >= nextThrowTime)
        {
            if (playerEnergy.HasEnough(rock.baseEnergyCost) && !player.isWalled)
            {
                Vector2 spawnPosition = throwDirection.position;

                // Validate if spawn point is inside a wall
                bool spawnInsidePlatform = Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, platformLayer) != null;
                if (spawnInsidePlatform) return;

                player.fastFall = false;
                inThrowState = true;
                holdTime = 0f;
                player.ResetGravity();
                playerEnergy.StopPassiveRegen();

                rockInst = Instantiate(rockPrefab, spawnPosition, throwPoint.transform.rotation);

                if (rockInst != null)
                {
                    Rock newRockScript = rockInst.GetComponent<Rock>();

                    if (newRockScript != null)
                    {
                        playerEnergy.UseEnergy(newRockScript.baseEnergyCost);
                        newRockScript.SetOwner(this, playerEnergy, throwDirection);
                    }

                    armRender.enabled = true;
                }
            }
        }

        if (context.canceled && inThrowState)
        {
            FireRock();
        }
    }

    private void FireRock()
    {
        if (rockInst != null)
        {
            Rock newRock = rockInst.GetComponent<Rock>();
            if (newRock != null)
            {
                newRock.ReleaseRock(aimDirection);

                // Calculate exponential recoil pushback
                float recoilForce = baseRecoilForce * Mathf.Pow(recoilGrowthRate, newRock.currentRockStage);

                player.isKnockBacked = true;
                player.myRigidBody2D.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
                player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);
            }
        }

        nextThrowTime = Time.time + throwCooldown;
        ResetThrowState();
        playerEnergy.StartPassiveRegen();
    }

    public void ResetThrowState()
    {
        inThrowState = false;
        holdTime = 0f;
        armRender.enabled = false;
        rockInst = null;
        player.myAnimator.SetBool("ThrowState", false);
    }

    public void ForceRelease()
    {
        if (inThrowState)
            FireRock();
    }
}