using System.Collections;
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
    [SerializeField] private float knockBackForce;
    [SerializeField] private float delayInRock;

    private GameObject rockInst;
    private Rock.RockType newRockType;

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
    }
    public void OnThrow(InputAction.CallbackContext context)
    {
        if (EnergyManager.Instance.currentEnergy > 0f && !player.isWalled && !player.IsWallSliding())
        {
            if (context.performed)
            {
                newRockType = Rock.RockType.Level1;
                rockInst = Instantiate(rockPrefab, throwDirection.position, throwPoint.transform.rotation);
                rockInst.transform.SetParent(throwDirection);
                rockInst.GetComponent<Rigidbody2D>().gravityScale = 0f;
                inThrowState = true;
                armRender.enabled = true;
                player.fastFall = false;

                player.myRigidBody2D.gravityScale = 0f;
                player.myRigidBody2D.linearVelocityY = 0f;
                player.myRigidBody2D.linearVelocityX = 0f;

                StartCoroutine(IncreaseRock());
            }

            if (context.canceled)
            {
                StopAllCoroutines();

                if (rockInst != null)
                {
                    rockInst.transform.SetParent(null);
                    Rock newRock = rockInst.GetComponent<Rock>();
                    newRock.rockType = newRockType;
                    newRock.InitializeRockStats();
                }
                player.isKnockBacked = true;

                player.myRigidBody2D.AddForce(-MouseDirection.Instance.direction * knockBackForce, ForceMode2D.Impulse);


                if (rock.rockType == Rock.RockType.Level1)
                    EnergyManager.Instance.UseEnergy(rock.level1EnergyCost);
                else if (rock.rockType == Rock.RockType.Level2)
                    EnergyManager.Instance.UseEnergy(rock.level2EnergyCost);
                else if (rock.rockType == Rock.RockType.Level3)
                    EnergyManager.Instance.UseEnergy(rock.level3EnergyCost);
                else if (rock.rockType == Rock.RockType.Level4)
                    EnergyManager.Instance.UseEnergy(rock.level4EnergyCost);
                else if (rock.rockType == Rock.RockType.Level5)
                    EnergyManager.Instance.UseEnergy(rock.level5EnergyCost);

                inThrowState = false;
                armRender.enabled = false;
                player.ResetGravity();
                player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);
            }
        }
    }
    private IEnumerator IncreaseRock()
    {
        yield return new WaitForSeconds(delayInRock);
        newRockType = Rock.RockType.Level2;
        rockInst.transform.localScale = new Vector3(1.5f, 1.5f, 0f);
        yield return new WaitForSeconds(delayInRock);
        newRockType = Rock.RockType.Level3;
        rockInst.transform.localScale = new Vector3(2f, 2f, 0f);
        yield return new WaitForSeconds(delayInRock);
        newRockType = Rock.RockType.Level4;
        rockInst.transform.localScale = new Vector3(2.5f, 2.5f, 0f);
        yield return new WaitForSeconds(delayInRock);
        newRockType = Rock.RockType.Level5;
        rockInst.transform.localScale = new Vector3(3f, 3f, 0f);
    }
}
