using UnityEngine;
using UnityEngine.InputSystem;

public class RockThrow : MonoBehaviour
{
    private PlayerController player;
    public bool inThrowState;
    private float angle;

    [SerializeField] private SpriteRenderer armRender;
    [SerializeField] private Rock rock;
    [SerializeField] private GameObject throwPoint;
    [SerializeField] private Transform throwDirection;
    [SerializeField] private GameObject rockPrefab;

    private void Start()
    {
        player = GetComponent<PlayerController>();
    }
    private void Update()
    {
        HandleRockDirection();
        player.playerAnimator.SetBool("ThrowState", inThrowState);
    }

    private void HandleRockDirection()
    {
       throwPoint.transform.right = MouseDirection.Instance.direction;

       angle = Mathf.Atan2(MouseDirection.Instance.direction.y, MouseDirection.Instance.direction.x) * Mathf.Rad2Deg;

       Vector3 localScale = Vector3.one;

       if(angle > 90 || angle < -90)
       {
           localScale.y = -1f;
       }
       else
       {
           localScale.y = 1f;
       }

       throwDirection.localScale = localScale;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if(EnergyManager.Instance.currentEnergy > 0f && !player.isWalled && !player.IsWallSliding()) 
        { 
            if (context.performed)  
            { 
                inThrowState = true;
                armRender.enabled = true;
                player.myRigidBody2D.gravityScale = 0f;
                player.myRigidBody2D.linearVelocityY = 0f;
                player.myRigidBody2D.linearVelocityX = 0f;
            }
            if (context.canceled)
            {
                Instantiate(rockPrefab, throwDirection.position, throwPoint.transform.rotation);

                if (rock.rockType == Rock.RockType.Normal)
                    EnergyManager.Instance.UseEnergy(rock.normalEnergyCost);
                else if(rock.rockType == Rock.RockType.Boulder)
                    EnergyManager.Instance.UseEnergy(rock.boulderEnergyCost);

                inThrowState = false;
                armRender.enabled = false;
                player.ResetGravity();
            }
        }
    } 
}