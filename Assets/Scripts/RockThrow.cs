using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RockThrow : MonoBehaviour
{
    private PlayerController player;
    public bool inThrowState;
    private float angle;
    [SerializeField] private ParticleSystem particleSystems;
    [SerializeField] private SpriteRenderer armRender;
    [SerializeField] private Rock rock;
    [SerializeField] private GameObject throwPoint;
    [SerializeField] private Transform throwDirection;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private float knockBackForce;

    private GameObject rockInst;

    [Header("Cooldown Settings")]
    [SerializeField] private float throwCooldown = 1f;
    private float nextThrowTime;

    private void Start()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleRockDirection();
        player.playerAnimator.SetBool("ThrowState", inThrowState);

        if (inThrowState && rockInst != null)
        {
            rockInst.transform.position = throwDirection.position;
            rockInst.transform.rotation = throwPoint.transform.rotation;
        }
    }

    private void HandleRockDirection()
    {
        throwPoint.transform.right = MouseDirection.Instance.direction;

        if (MouseDirection.Instance.direction.x < 0)
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
            if (EnergyManager.Instance.currentEnergy >= rock.baseEnergyCost && !player.isWalled)
            {
                inThrowState = true;
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
                Vector2 finalDirection = throwPoint.transform.right;
                newRock.ReleaseRock(finalDirection);

                player.isKnockBacked = true;
                player.myRigidBody2D.AddForce(-finalDirection * knockBackForce, ForceMode2D.Impulse);
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