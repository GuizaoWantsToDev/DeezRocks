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

    [Header("=== PHYSICS & COMBAT ===")]
    [SerializeField] private float knockBackForce;

    [Header("=== COOLDOWN SETTINGS ===")]
    [SerializeField] private float throwCooldown = 1f;
    private float nextThrowTime;

    [Header("=== SPAWN VALIDATION ===")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float spawnCheckRadius = 0.3f;

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

        if (inThrowState && rockInst != null)
        {
            // Apagámos o transform.position daqui! A pedra agora move-se sozinha pelo SmoothDamp
            rockInst.transform.rotation = throwPoint.transform.rotation;
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
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                aimDirection = (mousePosition - (Vector2)transform.position).normalized;
            }
        }
        else
        { 
            if (rawAimInput.magnitude > 0.5f)
            {
                aimDirection = rawAimInput.normalized;
            }
        }

        throwPoint.transform.right = aimDirection;

        if (aimDirection.x < 0)
        {
            armRender.flipY = true;
        }
        else
        {
            armRender.flipY = false;
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState && Time.time >= nextThrowTime)
        {
            if (playerEnergy.HasEnough(rock.baseEnergyCost) && !player.isWalled)
            {
                Vector2 spawnPosition = throwDirection.position;
                bool spawnInsidePlatform = Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, platformLayer) != null;

                if (spawnInsidePlatform) return;

                player.fastFall = false;
                inThrowState = true;
                player.ResetGravity();
                playerEnergy.StopPassiveRegen();

                rockInst = Instantiate(rockPrefab, spawnPosition, throwPoint.transform.rotation);

                if (rockInst != null)
                {
                    Rock newRockScript = rockInst.GetComponent<Rock>();
                    playerEnergy.UseEnergy(newRockScript.baseEnergyCost);

                    if (newRockScript != null)
                    {
                        // ATENÇÃO: Agora passamos o throwDirection (a mão) para a pedra seguir!
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

                player.isKnockBacked = true;
                player.myRigidBody2D.AddForce(-aimDirection * knockBackForce, ForceMode2D.Impulse);
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