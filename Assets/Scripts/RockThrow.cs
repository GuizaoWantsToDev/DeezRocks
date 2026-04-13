using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RockThrow : MonoBehaviour
{
    [Header("=== COMPONENTS ===")]
    private PlayerController player;
    private PlayerInput playerInput;

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

    private void Start()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        HandleAimDirection();
        player.playerAnimator.SetBool("ThrowState", inThrowState);

        if (inThrowState && rockInst != null)
        {
            rockInst.transform.position = throwDirection.position;
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
            if (rawAimInput.sqrMagnitude > 0.01f)
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
        if (context.performed && !inThrowState && Time.time >= nextThrowTime && !player.fastFall)
        {
            if (EnergyManager.Instance.currentEnergy >= rock.baseEnergyCost && !player.isWalled)
            {
                inThrowState = true;
                player.ResetGravity();
                EnergyManager.Instance.StopPassiveRegen();

                rockInst = Instantiate(rockPrefab, throwDirection.position, throwPoint.transform.rotation);

                if (rockInst != null)
                {
                    Rock newRockScript = rockInst.GetComponent<Rock>();
                    if (newRockScript != null) newRockScript.SetThrowReference(this);
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
        EnergyManager.Instance.StartPassiveRegen();
    }

    public void ResetThrowState()
    {
        inThrowState = false;
        armRender.enabled = false;
        rockInst = null;
        player.playerAnimator.SetBool("ThrowState", false);
    }

    public void ForceRelease()
    {
        if (inThrowState)
            FireRock();
    }
}