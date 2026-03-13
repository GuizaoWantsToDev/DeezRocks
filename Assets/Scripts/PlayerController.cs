using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] public Rigidbody2D myRigidBody2D;
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    [SerializeField] private Transform myTransform;
    [SerializeField] private TrailRenderer myTrailRenderer;

    [Header("Movement Settings")]
    [SerializeField] private float mSpeed; 
    [SerializeField] private float maxVerticalSpeed;
    private float inputValue;

    [Header("Jump Settings")]
    [SerializeField]
    private float jumpPower;
    public int maxJumps = 2;
    private int jumpsRemaining;


    [Header("Dash Settings")]
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCoolDown;
    [SerializeField] private float dashingPower;
        
    private bool canDash, isDashing;
    private bool dashOnCooldown;

    [Header("Wall Interaction")]
    [SerializeField] private float wallSlidingSpeed;
    public bool isWalled;

    [SerializeField] private float wallJumpTime;

    [SerializeField]
    private Vector2 wallJumpPower;
    private Vector2 wallJumpDirection;
    private bool isWallJumping;
    private bool canWallJump;

    [Header("Collision Checks")]
    private bool isGrounded;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private Vector2 groundCheckSize;

    [SerializeField] private Transform groundCheck;

    [SerializeField] private LayerMask wallLayer;

    [SerializeField] private Vector2 wallCheckSize;

    [SerializeField] private Transform wallCheckHead,wallCheckFoot;

    [Header("Physics")]
    [SerializeField] private float playerGravity;

    //Animation

    [SerializeField]
    public Animator playerAnimator;
    public bool fastFall;
    private RockThrow rockThrow;
    public bool isKnockBacked;
    [SerializeField] public float knockBackTime;
    private bool canCancel;

    private void Start()
    {
        GameManager.Instance.AddPlayer(gameObject);
        rockThrow= GetComponent<RockThrow>();
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
        }
        playerAnimator.SetBool("IsRunning", inputValue != 0f);
    }  

    public void OnJump(InputAction.CallbackContext context)
    {
        if (jumpsRemaining > 0 && !isWalled && !rockThrow.inThrowState)
        {
            if (context.performed && !isDashing)
            {
                myRigidBody2D.linearVelocityY = 0f;
                myRigidBody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                jumpsRemaining--;
            } 
        }
        else if (context.performed && canWallJump && isWalled)
        { 
            myRigidBody2D.linearVelocityY = 0f;
            isWallJumping = true;
            myTransform.right = -myTransform.right;

            myRigidBody2D.AddForce(wallJumpDirection * wallJumpPower, ForceMode2D.Impulse);
            

            Invoke(nameof(CancelWallJump), wallJumpTime);
        }
    }
    public void OnFall(InputAction.CallbackContext context)
    {
        if (context.performed && !isWalled && !rockThrow.inThrowState)
        {
            fastFall = true;
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
        fastFall = false;
        myRigidBody2D.gravityScale = 0f;
        myRigidBody2D.linearVelocity = new Vector2(myTransform.right.x * dashingPower, 0f);
        myTrailRenderer.emitting = true;

        yield return new WaitForSeconds(dashTime);

        myTrailRenderer.emitting = false;
        ResetGravity();
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

    public void CancelKnockBack()
    {
        canCancel = true;
    }
    private void CancelWallJump()
    {
        isWallJumping = false;
    }
    private void GroundCheck()
    {
        if (Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null)
        { 
            isGrounded = true; 
            fastFall = false;

            if(myRigidBody2D.linearVelocityY == 0f)
            jumpsRemaining = maxJumps; 
        }
        else
        {
            isGrounded = false;
        }
    }
    public bool IsWallSliding()
    {
        return Physics2D.OverlapBox(wallCheckHead.position, wallCheckSize, 0f, wallLayer) != null && Physics2D.OverlapBox(wallCheckFoot.position, wallCheckSize, 0f, wallLayer) != null;
    } 

    private void CheckFlip()
    {
        if (inputValue * myTransform.right.x < 0f && !rockThrow.inThrowState)
        {
            myTransform.right = -myTransform.right;
        }
    }
    public void ResetGravity()
    {
        myRigidBody2D.gravityScale = playerGravity;
    }

    public void Die()
    {
        Destroy(gameObject);
        GameManager.Instance.RemovePlayer(gameObject);
    }

    private void FixedUpdate()
    { 
        if (isDashing)
        {
            return;
        }
        if (!isWallJumping && !rockThrow.inThrowState && !isKnockBacked) 
        {
            myRigidBody2D.linearVelocityX = inputValue * mSpeed;
        }

        if (inputValue != 0 && canCancel)
        {
            isKnockBacked = false;
            canCancel = false;
        }

        if (fastFall == true)
        {
            myRigidBody2D.linearVelocityY = -maxVerticalSpeed;
        }
        myRigidBody2D.linearVelocity = new Vector2(myRigidBody2D.linearVelocityX, Mathf.Clamp(myRigidBody2D.linearVelocityY, -maxVerticalSpeed, maxVerticalSpeed));
        jumpsRemaining = Mathf.Clamp(jumpsRemaining, 0, maxJumps);

        playerAnimator.SetFloat("IsJumping", myRigidBody2D.linearVelocityY);
        playerAnimator.SetBool("IsWallSliding", IsWallSliding());
        playerAnimator.SetFloat("IsFalling", myRigidBody2D.linearVelocityY);
        playerAnimator.SetBool("IsGrounded", isGrounded);

            if (IsWallSliding() && inputValue != 0)
            {
                isWalled = true;
                jumpsRemaining = 1;
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
