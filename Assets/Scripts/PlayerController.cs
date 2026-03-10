using System.Collections;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private Rigidbody2D myRigidBody2D;
    [SerializeField]
    private SpriteRenderer mySpriteRenderer;

    [SerializeField]
    private Transform myTransform;

    [SerializeField]
    private TrailRenderer myTrailRenderer;

    [Header("Movement Settings")]
    [SerializeField]
    private float mSpeed;
    private float inputValue;

    [Header("Jump Settings")]
    [SerializeField]
    private float jumpPower;
    public int maxJumps = 2;
    private int jumpsRemaining;


    [Header("Dash Settings")]
    [SerializeField]
    private float dashTime;

    [SerializeField]
    private float dashCoolDown;

    [SerializeField]
    private float dashingPower;

    private bool canDash, isDashing;
    private bool dashOnCooldown;

    [Header("Wall Interaction")]
    [SerializeField]
    private float wallSlidingSpeed;
    private bool isWalled;

    [SerializeField]
    private float wallJumpTime;

    [SerializeField]
    private Vector2 wallJumpPower;
    private Vector2 wallJumpDirection;
    private bool isWallJumping;
    private bool canWallJump;

    [Header("Collision Checks")]
    private bool isGrounded;
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private Vector2 groundCheckSize;

    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private LayerMask wallLayer;

    [SerializeField]
    private Vector2 wallCheckSize;

    [SerializeField]
    private Transform wallCheckHead,wallCheckFoot;

    [Header("Physics")]
    [SerializeField]
    private float playerGravity;

    //Animation

    [SerializeField]
    private Animator playerAnimator;

    private void Start()
    {
        GameManager.Instance.AddPlayer(gameObject);
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            inputValue = context.ReadValue<float>();
        }
        if (context.canceled)
        {
            inputValue = 0f;

            if (!isWallJumping) 
            {
                myRigidBody2D.linearVelocityX = 0f;
            }
        }
        playerAnimator.SetBool("IsRunning", inputValue != 0f);
    }  

    public void OnJump(InputAction.CallbackContext context)
    {
        if (jumpsRemaining > 0 && !isWalled)
        {
            if (context.performed && !isDashing)
            {
                myRigidBody2D.linearVelocityY = 0f;
                myRigidBody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                jumpsRemaining--;
            }
            else if (context.canceled)
            {
                myRigidBody2D.linearVelocity = new Vector2(myRigidBody2D.linearVelocity.x, myRigidBody2D.linearVelocity.y * 0.5f);
                jumpsRemaining--;
            }
        }
        else if (context.performed && canWallJump && isWalled)
        { 
            isWallJumping = true;
            myTransform.right = -myTransform.right;
            myRigidBody2D.linearVelocityY = 0f;

            myRigidBody2D.AddForce(wallJumpDirection * wallJumpPower, ForceMode2D.Impulse);
            

            Invoke(nameof(CancelWallJump), wallJumpTime);
        }
    }
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            ContactPoint2D contact = collision.GetContact(0);
            wallJumpDirection = contact.normal + Vector2.up;
        }
    }
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && !isWalled)
        {
            StartCoroutine(Dash());
        }
    }
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        myRigidBody2D.gravityScale = 0f;
        myRigidBody2D.linearVelocity = new Vector2(myTransform.right.x * dashingPower, 0f);
        myTrailRenderer.emitting = true;

        yield return new WaitForSeconds(dashTime);

        myTrailRenderer.emitting = false;
        myRigidBody2D.gravityScale = playerGravity;
        isDashing = false;

        dashOnCooldown = true;
        yield return new WaitForSeconds(dashCoolDown);
        dashOnCooldown = false;
    }

    private void WallSlide()
    {
        if (isWalled && !isGrounded)
        {
            myRigidBody2D.linearVelocity = new Vector2(myRigidBody2D.linearVelocity.x, Mathf.Max(myRigidBody2D.linearVelocityY, -wallSlidingSpeed));
            
        }
    }

    private void WallJump()
    {
        if (IsWallSliding())
        {
            isWallJumping = false;
            canWallJump = true;

            CancelInvoke(nameof(CancelWallJump));
        }
        else if (!IsWallSliding()) 
        {
            canWallJump = false;
        }
    }

    private void CancelWallJump()
    {
        isWallJumping = false;
    }
    private void GroundCheck()
    {
        if (Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null)
        {
            myRigidBody2D.linearVelocityX = inputValue * mSpeed;
            jumpsRemaining = maxJumps;
            isGrounded = true; 
        }
        else
        {
            isGrounded = false;
        }
    }
    private bool IsWallSliding()
    {
        return Physics2D.OverlapBox(wallCheckHead.position, wallCheckSize, 0f, wallLayer) != null && Physics2D.OverlapBox(wallCheckFoot.position, wallCheckSize, 0f, wallLayer) != null;
    }
    /*private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
    }*/


    private void CheckFlip()
    {
        if (inputValue * myTransform.right.x < 0f)
        {
            myTransform.right = -myTransform.right;
        }
    }

    public void Die()
    {
        GameObject.Destroy(gameObject);
    }

    private void FixedUpdate()
    { 
        if (isDashing)
        {
            return;
        }
        if (!isWallJumping && inputValue != 0) 
        {
            myRigidBody2D.linearVelocityX = inputValue * mSpeed;
        }

        playerAnimator.SetFloat("IsJumping", myRigidBody2D.linearVelocityY);
        playerAnimator.SetBool("IsWallSliding", IsWallSliding());
        playerAnimator.SetFloat("IsFalling", myRigidBody2D.linearVelocityY);
        playerAnimator.SetBool("IsGrounded", isGrounded);

            if (IsWallSliding() && inputValue != 0)
            {
                isWalled = true;

            }
            else if (!IsWallSliding())
            {
                isWalled = false;
            }

            GroundCheck();
            WallJump();
            WallSlide();
            CheckFlip();

            if (!canDash && !isDashing && !dashOnCooldown)
            {
                if (isGrounded || isWalled)
                {
                    canDash = true;
                }
            }
        }
    }
