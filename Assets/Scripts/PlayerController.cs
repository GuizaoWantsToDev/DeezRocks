using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("=== COMPONENTS ===")]
    [SerializeField] public Rigidbody2D myRigidBody2D;
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    [SerializeField] private Transform myTransform;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] public Animator playerAnimator;

    [Header("=== MOVEMENT & PHYSICS ===")]
    [SerializeField] private float mSpeed;
    [SerializeField] private float maxVerticalSpeed;
    [SerializeField] private float playerGravity;

    [Header("=== JUMP SETTINGS ===")]
    [SerializeField] private float jumpPower;
    public int maxJumps = 2;

    [Header("=== DASH SETTINGS ===")]
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCoolDown;
    [SerializeField] private float dashingPower;

    [Header("=== WALL INTERACTION ===")]
    [SerializeField] private float wallSlidingSpeed;
    [SerializeField] private float wallJumpTime;
    [SerializeField] private Vector2 wallJumpPower;
    [SerializeField] private float wallDetachBufferTime = 0.15f;

    [Header("=== SLOPES & FRICTION ===")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeCheckDistance = 0.5f;
    [SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;

    [Header("=== COLLISION SETUP ===")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [Space(5)]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize;
    [Space(5)]
    [SerializeField] private Transform wallCheckHead;
    [SerializeField] private Transform wallCheckFoot;
    [SerializeField] private Vector2 wallCheckSize;

    [Header("=== COMBAT & LIVE STATES ===")]
    [SerializeField] public float knockBackTime;

    public bool isWalled;
    public bool fastFall;
    public bool isKnockBacked;

    // --- VARIÁVEIS PRIVADAS ---
    private float inputValue;
    private int jumpsRemaining;
    private bool isJumping;
    private bool canDash, isDashing, dashOnCooldown;
    private Vector2 wallJumpDirection;
    private bool isWallJumping;
    private bool canWallJump;
    private float currentWallDetachTimer;
    private bool isGrounded;
    private bool isOnSlope;
    private bool canWalkOnSlope;
    private float slopeDownAngle;
    private float slopeSideAngle;
    private float lastSlopeAngle;
    private Vector2 slopeNormalPerp;
    private CapsuleCollider2D cc;
    private RockThrow rockThrow;
    private bool canCancel;
    private bool isTouchingWallAnim;


    private void Start()
    {
        if (GameManager.Instance != null) 
            GameManager.Instance.AddPlayer(gameObject);

        rockThrow = GetComponent<RockThrow>();
        cc = GetComponent<CapsuleCollider2D>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed) inputValue = context.ReadValue<float>();
        if (context.canceled) inputValue = 0f;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && !isDashing && !rockThrow.inThrowState)
        {
            if (jumpsRemaining > 0 && !isWalled) 
                Jump();
            else if (canWallJump && isWalled)
                WallJump();
        }
    }

    public void OnFall(InputAction.CallbackContext context)
    {
        if (context.performed && !isWalled && !rockThrow.inThrowState) 
            fastFall = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && !isWalled && !rockThrow.inThrowState) 
            StartCoroutine(Dash());
    }

    private void Jump()
    {
        isJumping = true;
        isGrounded = false;
        myRigidBody2D.linearVelocityY = 0f;
        myRigidBody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        jumpsRemaining--;
        Invoke(nameof(ResetJumpState), 0.15f);
    }

    private void WallJump()
    {
        isWallJumping = true;
        isJumping = true;

        wallJumpDirection = new Vector2(-myTransform.right.x, 1f).normalized;
        myTransform.right = -myTransform.right;

        myRigidBody2D.linearVelocity = Vector2.zero;
        myRigidBody2D.AddForce(wallJumpDirection * wallJumpPower, ForceMode2D.Impulse);

        Invoke(nameof(CancelWallJump), wallJumpTime);
        Invoke(nameof(ResetJumpState), 0.15f);
    }

    private void ResetJumpState()
    {
        isJumping = false;
    }
    private void CancelWallJump() 
    {
        isWallJumping = false;
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
        isDashing = false;
        dashOnCooldown = true;

        yield return new WaitForSeconds(dashCoolDown);
        dashOnCooldown = false;
    }
    private void CheckGround()
    {
        bool rawGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;

        isGrounded = rawGrounded && !(isTouchingWallAnim && myRigidBody2D.linearVelocityY < -0.1f);

        if (myRigidBody2D.linearVelocityY <= 0.0f) isJumping = false;

        if (isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle)
        {
            jumpsRemaining = maxJumps;
            fastFall = false;
        }
    }

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - new Vector3(0.0f, cc.size.y / 2f);

        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, myTransform.right, slopeCheckDistance, groundLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -myTransform.right, slopeCheckDistance, groundLayer);

        if (slopeHitFront) slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        else if (slopeHitBack) slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        else slopeSideAngle = 0.0f;

        isOnSlope = slopeHitFront || slopeHitBack;

        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, groundLayer);
        if (hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeDownAngle != lastSlopeAngle) isOnSlope = true;
            lastSlopeAngle = slopeDownAngle;
        }

        canWalkOnSlope = slopeDownAngle <= maxSlopeAngle && slopeSideAngle <= maxSlopeAngle;
        myRigidBody2D.sharedMaterial = (isOnSlope && canWalkOnSlope && inputValue == 0f) ? fullFriction : noFriction;
    }

    public bool IsWallSliding()
    {
        return Physics2D.OverlapBox(wallCheckHead.position, wallCheckSize, 0f, wallLayer) != null && Physics2D.OverlapBox(wallCheckFoot.position, wallCheckSize, 0f, wallLayer) != null;
    }

    private void HandleWallLogic()
    {
        isTouchingWallAnim = IsWallSliding();

        if (isTouchingWallAnim && inputValue != 0)
        {
            isWalled = true;
            jumpsRemaining = 1;
            canWallJump = true;
            currentWallDetachTimer = wallDetachBufferTime;
        }
        else if (!isTouchingWallAnim)
        {
            if (currentWallDetachTimer > 0)
            {
                currentWallDetachTimer -= Time.deltaTime;
                isWalled = true;
            }
            else
            {
                isWalled = false;
                canWallJump = false;
            }
        }
    }

    private void ApplyMovement()
    {
        if (isWalled)
        {
            bool pushingWall = (inputValue > 0 && myTransform.right.x > 0) || (inputValue < 0 && myTransform.right.x < 0);
            float xVel = pushingWall ? 0f : inputValue * mSpeed;
            myRigidBody2D.linearVelocity = new Vector2(xVel, Mathf.Max(myRigidBody2D.linearVelocityY, -wallSlidingSpeed));
            return;
        }

        if (isGrounded && !isJumping)
        {
            if (isOnSlope && canWalkOnSlope)
            {
                myRigidBody2D.linearVelocity = inputValue == 0f ? Vector2.zero : new Vector2(mSpeed * slopeNormalPerp.x * -inputValue, mSpeed * slopeNormalPerp.y * -inputValue);
            }
            else
            {
                myRigidBody2D.linearVelocity = new Vector2(mSpeed * inputValue, 0.0f);
            }
        }
        else if (!isGrounded)
        {
            myRigidBody2D.linearVelocity = new Vector2(mSpeed * inputValue, myRigidBody2D.linearVelocityY);
        }
    }

    private void CheckFlip()
    {
        if (rockThrow.inThrowState)
        {
            float mousePosX = MouseDirection.Instance.direction.x;
            myTransform.right = mousePosX > 0 ? Vector3.right : (mousePosX < 0 ? Vector3.left : myTransform.right);
            return;
        }

        if (isWalled) 
            return;

        if (inputValue * myTransform.right.x < 0f) 
            myTransform.right = -myTransform.right;
    }

    private void UpdateAnimations()
    {
        bool isActuallyWallSliding = isTouchingWallAnim && myRigidBody2D.linearVelocityY < -0.1f && !isGrounded;
        bool isMoving = Mathf.Abs(inputValue) > 0f || Mathf.Abs(myRigidBody2D.linearVelocityX) > 0.5f;

        playerAnimator.SetBool("IsWallSliding", isActuallyWallSliding);
        playerAnimator.SetBool("IsRunning", isMoving && isGrounded);
        playerAnimator.SetBool("IsGrounded", isGrounded);

        bool jumping = !isGrounded && !isActuallyWallSliding && myRigidBody2D.linearVelocityY > 0.5f;
        bool falling = !isGrounded && !isActuallyWallSliding && myRigidBody2D.linearVelocityY < -0.5f;

        playerAnimator.SetFloat("IsJumping", jumping ? myRigidBody2D.linearVelocityY : 0f);
        playerAnimator.SetFloat("IsFalling", falling ? myRigidBody2D.linearVelocityY : 0f);
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        HandleWallLogic();
        CheckGround();
        SlopeCheck();

        float currentGrav = playerGravity;
        if (rockThrow.inThrowState)
        {
            myRigidBody2D.linearVelocity = Vector2.zero;
            currentGrav = 0f;
        }
        else if (isGrounded && isOnSlope && inputValue == 0f && !isJumping)
        {
            currentGrav = 0f;
        }

        myRigidBody2D.gravityScale = currentGrav;

        if (!rockThrow.inThrowState && !isWallJumping)
        {
            if (isKnockBacked)
            {
                if (isWalled || IsWallSliding() || isGrounded)
                {
                    myRigidBody2D.linearVelocity = new Vector2(0f, myRigidBody2D.linearVelocityY);
                    isKnockBacked = false;
                    canCancel = false;
                }
            }
            else
            {
                ApplyMovement();
            }
        }

        if (inputValue != 0 && canCancel && isKnockBacked)
        {
            isKnockBacked = false;
            canCancel = false;
        }

        if (fastFall) myRigidBody2D.linearVelocityY = -maxVerticalSpeed;

        if (!isOnSlope)
        {
            float clampedY = Mathf.Clamp(myRigidBody2D.linearVelocityY, -maxVerticalSpeed, maxVerticalSpeed);
            myRigidBody2D.linearVelocity = new Vector2(myRigidBody2D.linearVelocityX, clampedY);
        }

        jumpsRemaining = Mathf.Clamp(jumpsRemaining, 0, maxJumps);

        UpdateAnimations();
        CheckFlip();

        if (!isDashing && !dashOnCooldown && (isGrounded || isWalled)) canDash = true;
    }
    public void CancelKnockBack()
    {
        canCancel = true;
    }

    public void Die()
    {
        if (GameManager.Instance != null) GameManager.Instance.RemovePlayer(gameObject);
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (groundCheck != null) Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);

        if (wallCheckHead != null && wallCheckFoot != null)
        {
            Gizmos.DrawWireCube(wallCheckHead.position, wallCheckSize);
            Gizmos.DrawWireCube(wallCheckFoot.position, wallCheckSize);
        }

        if (Application.isPlaying && isWalled)
        {
            Vector2 visualJumpDir = new Vector2(-transform.right.x, 1f).normalized;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + (Vector3)visualJumpDir * 2f;

            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawSphere(endPos, 0.1f);
        }
    }
}