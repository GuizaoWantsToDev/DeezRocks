using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Central controller managing player physics, mobility abilities, state validation, and animations
public class PlayerController : MonoBehaviour
{
    [Header("=== COMPONENTS ===")]
    [SerializeField] public Rigidbody2D myRigidBody2D;
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    [SerializeField] private Transform myTransform;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] public Animator myAnimator;

    [Header("=== MOVEMENT & PHYSICS ===")]
    [SerializeField] private float mSpeed;
    [SerializeField] private float maxVerticalSpeed;
    [SerializeField] private float fastFallImpulse;
    [SerializeField] private float playerGravity;
    private float currentGravity;

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

    [Header("=== HOLD FALL SETTINGS ===")]
    [SerializeField] private float minHoldFallGravity = 0.05f;
    [SerializeField] private float holdFallGrowthRate = 1.15f;
    [SerializeField] private float maxHoldFallSpeed = 1.5f;

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

    [Header("=== KNOCKBACK SLIDE ===")]
    // How fast the player decelerates on the ground after being knocked back
    [SerializeField] private float groundKnockbackDeceleration = 10f;

    // Lets RockThrow check if the player is on the ground before applying recoil
    public bool IsGrounded => isGrounded;
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
    private bool isHoldingFall;
    private Coroutine dashCoroutine;
    private bool knocked;
    private float knockedTimer; 

    private void Start()
    {
        // Register this player instance with the global GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.AddPlayer(gameObject);

        rockThrow = GetComponent<RockThrow>();
        cc = GetComponent<CapsuleCollider2D>();
        ResetGravity();
    }

    // Handles movement input mapping via the Input System
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            inputValue = context.ReadValue<float>();
        }
        if (context.canceled) inputValue = 0f;
    }

    // Handles jump input, choosing between normal jumps and wall jumps
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && !isDashing && !rockThrow.inThrowState)
        {
            if (canWallJump)
            {
                WallJump();
            }
            else if (jumpsRemaining > 0 && !isWalled)
            {
                Jump();
            }
        }
    }

    // Listens for the downward directional input to trigger the fast fall mechanic
    public void OnFall(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isHoldingFall = true;
            // Only trigger fast fall if the player is airborne and not doing other heavy actions
            if (!isWalled && !rockThrow.inThrowState && !isDashing)
            {
                ExecuteFastFall();
            }
        }
        else if (context.canceled)
        {
            isHoldingFall = false;
            ResetGravity();
        }
    }

    // Overrides gravity and applies a downward impulse
    private void ExecuteFastFall()
    {
        fastFall = true;
        currentGravity = playerGravity * 4f;
        myRigidBody2D.AddForce(Vector2.down * fastFallImpulse, ForceMode2D.Impulse);
    }

    // Triggers the dash sequence
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && !isWalled && !rockThrow.inThrowState && !isWallJumping)
        {
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            dashCoroutine = StartCoroutine(Dash());
        }
    }

    // Applies upward force for a standard jump and updates the jump counter
    private void Jump()
    {
        isJumping = true;
        isGrounded = false;
        ResetGravity();
        myRigidBody2D.linearVelocityY = 0f; // Reset vertical momentum
        myRigidBody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        jumpsRemaining--;
        Invoke(nameof(ResetJumpState), 0.15f); // Buffer to prevent immediate re-grounding
    }

    // Calculates and applies directional force to jump away from a wall surface
    private void WallJump()
    {
        canWallJump = false;
        isWallJumping = true;
        isJumping = true;
        ResetGravity();

        float jumpDirX;
        if (isTouchingWallAnim)
        {
            jumpDirX = -myTransform.right.x;
        }
        else
        {
            jumpDirX = myTransform.right.x;
        }

        wallJumpDirection = new Vector2(jumpDirX, 1f).normalized;
        myTransform.right = new Vector3(jumpDirX, 0f, 0f);

        myRigidBody2D.linearVelocity = Vector2.zero; // Clear momentum
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

    // Coroutine managing the dash duration, linear physics override, trail effect, and cooldown
    private IEnumerator Dash()
    {     
        canDash = false;
        isDashing = true;
        ResetGravity();

        myRigidBody2D.gravityScale = 0f; // Disable gravity during dash
        myRigidBody2D.linearVelocity = new Vector2(myTransform.right.x * dashingPower, 0f);
        myTrailRenderer.emitting = true;

        yield return new WaitForSeconds(dashTime);

        myTrailRenderer.emitting = false;
        isDashing = false;

        // Re-apply fast fall if the player is still holding the input
        if (isHoldingFall && !isWalled && !rockThrow.inThrowState && !isGrounded)
        {
            ExecuteFastFall();
        }

        dashOnCooldown = true;
        yield return new WaitForSeconds(dashCoolDown);
        dashOnCooldown = false;
    }

    private void ResetDashCooldown()
    {
        dashOnCooldown = false;
    }

    // Validates if the player is touching the ground layer using an OverlapBox projection
    private void CheckGround()
    {
        bool rawGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;
        isGrounded = rawGrounded && !(isTouchingWallAnim && myRigidBody2D.linearVelocityY < -0.5f);

        if (myRigidBody2D.linearVelocityY <= 0.1f) isJumping = false;

        // Reset jumps and states upon safely landing
        if (isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle)
        {
            jumpsRemaining = maxJumps;
            fastFall = false;
        }
    }

    // Fires Raycasts downward and forward to map the exact angle of the surface beneath the player
    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - new Vector3(0.0f, cc.size.y / 2f);

        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, myTransform.right, slopeCheckDistance, groundLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -myTransform.right, slopeCheckDistance, groundLayer);

        if (slopeHitFront)
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        else if (slopeHitBack)
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        else
            slopeSideAngle = 0.0f;

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

        // Adjust friction material dynamically to prevent sliding down slopes when standing still
        if (isOnSlope && canWalkOnSlope && inputValue == 0f)
        {
            myRigidBody2D.sharedMaterial = fullFriction;
        }
        else
        {
            myRigidBody2D.sharedMaterial = noFriction;
        }
    }

    // Projects overlap boxes at head and foot levels to confirm a flat vertical wall surface
    public bool IsWallSliding()
    {
        return Physics2D.OverlapBox(wallCheckHead.position, wallCheckSize, 0f, wallLayer) != null && Physics2D.OverlapBox(wallCheckFoot.position, wallCheckSize, 0f, wallLayer) != null;
    }

    // Updates wall-related boolean logic based on player input and physical wall proximity
    private void HandleWallLogic()
    {
        isTouchingWallAnim = IsWallSliding();

        if (isTouchingWallAnim)
        {
            bool isPushingWall = (inputValue > 0 && myTransform.right.x > 0) || (inputValue < 0 && myTransform.right.x < 0);
            bool isPullingAway = (inputValue > 0 && myTransform.right.x < 0) || (inputValue < 0 && myTransform.right.x > 0);

            if (isPushingWall)
            {
                isWalled = true;
                jumpsRemaining = 1;
                canWallJump = true;
                currentWallDetachTimer = wallDetachBufferTime;
            }
            else if (isPullingAway)
            {
                isWalled = false;
            }
        }
        else
        {
            isWalled = false;
            if (currentWallDetachTimer > 0)
            {
                currentWallDetachTimer -= Time.deltaTime;
                canWallJump = true;
            }
            else
            {
                canWallJump = false;
            }
        }
    }

    // Translates input values into Rigidbody linear velocities depending on the current physical environment
    private void ApplyMovement()
    {
        if (isWalled)
        {
            // Limit vertical descent speed when sliding against a wall
            myRigidBody2D.linearVelocity = new Vector2(0f, Mathf.Max(myRigidBody2D.linearVelocityY, -wallSlidingSpeed));
            return;
        }

        if (isGrounded && !isJumping)
        {
            if (isOnSlope && canWalkOnSlope)
            {
                if (inputValue == 0f)
                {
                    myRigidBody2D.linearVelocity = Vector2.zero;
                }
                else
                {
                    myRigidBody2D.linearVelocity = new Vector2(mSpeed * slopeNormalPerp.x * -inputValue, mSpeed * slopeNormalPerp.y * -inputValue);
                }
            }
            else
            {
                // Standard flat ground movement
                myRigidBody2D.linearVelocity = new Vector2(mSpeed * inputValue, 0.0f);
            }
        }
        else if (!isGrounded)
        {
            // Air mobility retention
            myRigidBody2D.linearVelocity = new Vector2(mSpeed * inputValue, myRigidBody2D.linearVelocityY);
        }
    }

    // Flips the transform automatically based on input direction or aiming target
    private void CheckFlip()
    {
        if (rockThrow.inThrowState)
        {
            // Force character to face the crosshair while charging an attack
            if (rockThrow.aimDirection.x > 0 && myTransform.right.x < 0)
            {
                myTransform.right = Vector3.right;
            }
            else if (rockThrow.aimDirection.x < 0 && myTransform.right.x > 0)
            {
                myTransform.right = Vector3.left;
            }
            return;
        }

        if (isWallJumping) return;

        if (inputValue * myTransform.right.x < 0f)
            myTransform.right = -myTransform.right;
    }

    // Communicates boolean and float values to the Animator component to trigger correct sprites
    private void UpdateAnimations()
    {
        bool isActuallyWallSliding = isTouchingWallAnim && !isGrounded;
        bool isMoving = Mathf.Abs(inputValue) > 0f || Mathf.Abs(myRigidBody2D.linearVelocityX) > 0.5f;

        myAnimator.SetBool("IsWallSliding", isActuallyWallSliding && !isKnockBacked);
        myAnimator.SetBool("IsRunning", isMoving && isGrounded && !isKnockBacked);
        myAnimator.SetBool("IsGrounded", isGrounded && !isKnockBacked);

        bool jumping = !isGrounded && !isActuallyWallSliding && myRigidBody2D.linearVelocityY > 0.5f && !isOnSlope;
        bool falling = !isGrounded && !isActuallyWallSliding && myRigidBody2D.linearVelocityY < -0.1f;

        if(!isKnockBacked)
        {
            if (jumping)
            {
                myAnimator.SetFloat("IsJumping", myRigidBody2D.linearVelocityY);
            }
            else
            {
                myAnimator.SetFloat("IsJumping", 0f);
            }

            if (falling)
            {
                myAnimator.SetFloat("IsFalling", myRigidBody2D.linearVelocityY);
            }
            else
            {
                myAnimator.SetFloat("IsFalling", 0f);
            }
        }
    }

    #region 
    public void StartKnockedStage()
    {
        StartCoroutine(KnockedStage());
    }

    private IEnumerator KnockedStage()
    {
        knocked = true;
        myRigidBody2D.sharedMaterial.bounciness = 1f;

        yield return new WaitForSeconds(knockedTimer);

        knocked = false;
        myRigidBody2D.sharedMaterial.bounciness = 0f;
    }
    #endregion 

    // Main physics tick execution loop
    private void FixedUpdate()
    {
        if (knocked)
            return;

        myAnimator.SetBool("IsDashing", isDashing && !isKnockBacked);

        if (isDashing)
        {  
            if (IsWallSliding())
            {
                if (dashCoroutine != null) StopCoroutine(dashCoroutine);
                isDashing = false;
                myTrailRenderer.emitting = false;
                ResetGravity();
                dashOnCooldown = true;
                Invoke(nameof(ResetDashCooldown), dashCoolDown);
            }
            else
            {
                myRigidBody2D.linearVelocity = new Vector2(myTransform.right.x * dashingPower, 0f);
                return;
            }
        }

        HandleWallLogic();
        CheckGround();
        SlopeCheck();

        float gravToApply = currentGravity;

        // --- ROCK HOLDING SLOW FALL LOGIC ---
        // Dynamically reduces gravity to create a floating effect while aiming mid-air
        if (rockThrow.inThrowState)
        {
            if (fastFall) ResetGravity();

            // Cancel any upward momentum instantly when the player starts aiming
            if (myRigidBody2D.linearVelocityY > 0f)
            {
                myRigidBody2D.linearVelocity = Vector2.zero;
            }

            // Exponentially increase fall speed over time, clamped to a maximum value
            float holdFallGravity = minHoldFallGravity * Mathf.Pow(holdFallGrowthRate, rockThrow.holdTime);
            gravToApply = Mathf.Min(holdFallGravity, playerGravity);

            float clampedFallSpeed = Mathf.Max(myRigidBody2D.linearVelocityY, -maxHoldFallSpeed);

            // Force X velocity to 0 to prevent horizontal drifting while aiming
            myRigidBody2D.linearVelocity = new Vector2(0f, clampedFallSpeed);
        }
        else if (isGrounded && isOnSlope && inputValue == 0f && !isJumping)
        {
            gravToApply = 0f;
        }

        myRigidBody2D.gravityScale = gravToApply;

        if (!rockThrow.inThrowState && !isWallJumping)
        {
            if (isKnockBacked)
            {
                if (isGrounded)
                {
                    // Slide on the ground with deceleration instead of stopping immediately
                    float slidingVelocityX = Mathf.MoveTowards(
                        myRigidBody2D.linearVelocityX,
                        0f,
                        groundKnockbackDeceleration * Time.fixedDeltaTime
                    );
                    myRigidBody2D.linearVelocity = new Vector2(slidingVelocityX, myRigidBody2D.linearVelocityY);

                    // Auto-cancel once fully stopped AND the knockback timer has ended
                    if (Mathf.Abs(slidingVelocityX) < 0.05f && canCancel)
                    {
                        transform.rotation = Quaternion.Euler(0, 0, 0);
                        isKnockBacked = false;
                        canCancel = false;
                    }
                }

                // Stop immediately only when hitting a wall
                if (isWalled || IsWallSliding())
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

        // Player cancels knockback early by pressing a direction after the timer ends
        if (inputValue != 0 && canCancel && isKnockBacked)
        {
            isKnockBacked = false;
            canCancel = false;
        }

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

    public void ResetGravity()
    {
        currentGravity = playerGravity;
        fastFall = false;
    }

    public void CancelKnockBack()
    {
        canCancel = true;
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
            float jumpDirX;
            if (isTouchingWallAnim)
            {
                jumpDirX = -myTransform.right.x;
            }
            else
            {
                jumpDirX = myTransform.right.x;
            }

            Vector2 visualJumpDir = new Vector2(jumpDirX, 1f).normalized;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + (Vector3)visualJumpDir * 2f;

            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawSphere(endPos, 0.1f);
        }
    }
}