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
    [SerializeField] public Animator myAnimator;
    [SerializeField] public GameObject ragdoll;
    [SerializeField] private ParticleSystem myParticleSystem;
    [SerializeField] private GameObject myOtherParticleSystem;
    [SerializeField] private GameObject walkParticle;

    [Header("Movement")]
    [SerializeField] private float mSpeed;
    [SerializeField] private float maxVerticalSpeed;
    public bool canMove = true;

    [Header("Jump & Fast Fall")]
    [SerializeField] private float jumpPower;
    public int maxJumps = 2;
    [SerializeField] private float fastFallImpulse;
    [SerializeField] private float minHoldFallGravity = 0.05f;
    [SerializeField] private float holdFallGrowthRate = 1.15f;
    [SerializeField] private float maxHoldFallSpeed = 1.5f;

    [Header("Dash")]
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCoolDown;
    [SerializeField] private float dashingPower;

    [Header("Wall Interaction")]
    [SerializeField] private float wallSlidingSpeed;
    [SerializeField] private float wallJumpTime;
    [SerializeField] private Vector2 wallJumpPower;
    [SerializeField] private float wallDetachBufferTime = 0.15f;

    [Header("Slopes & Physics")]
    [SerializeField] private float playerGravity;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeCheckDistance = 0.5f;
    [SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;

    [Header("Collision Checks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize;
    [SerializeField] private Transform wallCheckHead;
    [SerializeField] private Transform wallCheckFoot;
    [SerializeField] private Vector2 wallCheckSize;

    [Header("Combat")]
    [SerializeField] public float knockBackTime;
    [SerializeField] private float groundKnockbackDeceleration = 10f;

    public bool isWalled;
    public bool fastFall;
    public bool isKnockBacked;
    public bool isKnocked = false;
    public bool IsGrounded => isGrounded;

    private float currentGravity;
    private float inputValue;
    private int jumpsRemaining;
    private bool isJumping;
    private bool canDash;
    private bool isDashing;
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
    private float knockedTimer;
    private bool ground;
    private Coroutine dashCoroutine;
    private Coroutine knockedCoroutine;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPlayer(gameObject);
        }

        rockThrow = GetComponent<RockThrow>();
        cc = GetComponent<CapsuleCollider2D>();
        ResetGravity();

        knockedTimer = MobilityAndCombatStats.Instance.knockedTimer;
        myRigidBody2D.sharedMaterial.bounciness = 0f;
    }

    private void PlayParticles(GameObject particle)
    {
        particle.SetActive(true);
    }

    private void StopParticles(GameObject particle)
    {
        particle.SetActive(false);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (isKnocked)
        {
            return;
        }

        if (context.performed)
        {
            inputValue = context.ReadValue<float>();
        }
        else if (context.canceled)
        {
            inputValue = 0f;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isKnocked || !context.performed || isDashing || rockThrow.inThrowState)
        {
            return;
        }

        if (canWallJump)
        {
            WallJump();
        }
        else if (jumpsRemaining > 0 && !isWalled)
        {
            Jump();
        }
    }

    #region FAST_FALL

    public void OnFall(InputAction.CallbackContext context)
    {
        if (isKnocked)
        {
            return;
        }

        if (context.performed)
        {
            isHoldingFall = true;

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

    private void ExecuteFastFall()
    {
        fastFall = true;
        currentGravity = playerGravity * 4f;
        myRigidBody2D.AddForce(Vector2.down * fastFallImpulse, ForceMode2D.Impulse);
    }

    #endregion

    #region DASH

    public void OnDash(InputAction.CallbackContext context)
    {
        if (isKnocked || !context.performed || !canDash || isWalled || rockThrow.inThrowState || isWallJumping)
        {
            return;
        }

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
        }

        dashCoroutine = StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        ResetGravity();

        myRigidBody2D.gravityScale = 0f;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDashSound();
        }

        myRigidBody2D.linearVelocity = new Vector2(myTransform.right.x * dashingPower, 0f);
        myTrailRenderer.emitting = true;

        yield return new WaitForSeconds(dashTime);

        myTrailRenderer.emitting = false;
        isDashing = false;

        if (isHoldingFall && !isWalled && !rockThrow.inThrowState && !isGrounded)
        {
            ExecuteFastFall();
        }

        yield return new WaitForSeconds(dashCoolDown);
    }

    #endregion

    private void Jump()
    {
        ground = false;
        isJumping = true;
        isGrounded = false;
        ResetGravity();
        myRigidBody2D.linearVelocityY = 0f;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PLayJumpSound();
        }

        myRigidBody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        jumpsRemaining--;
        Invoke(nameof(ResetJumpState), 0.15f);
    }

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

        myRigidBody2D.linearVelocity = Vector2.zero;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PLayJumpSound();
        }

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

    private void CheckGround()
    {
        bool rawGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;
        isGrounded = rawGrounded && !(isTouchingWallAnim && myRigidBody2D.linearVelocityY < -0.5f);

        if (myRigidBody2D.linearVelocityY <= 0.1f)
        {
            isJumping = false;
        }

        if (isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle && !isKnocked)
        {
            if (!ground)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayLandingSound();
                }
                ground = true;
            }

            canDash = true;
            PlayParticles(walkParticle);
            jumpsRemaining = maxJumps;
            fastFall = false;
        }
    }

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - new Vector3(0.0f, cc.size.y / 2f);

        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, myTransform.right, slopeCheckDistance, groundLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -myTransform.right, slopeCheckDistance, groundLayer);

        if (slopeHitFront)
        {
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else if (slopeHitBack)
        {
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0.0f;
        }

        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance * 1.5f, groundLayer);

        if (hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != lastSlopeAngle)
            {
                isOnSlope = true;
            }

            lastSlopeAngle = slopeDownAngle;
        }
        else
        {
            isOnSlope = false;
        }

        canWalkOnSlope = slopeDownAngle <= maxSlopeAngle && slopeSideAngle <= maxSlopeAngle;

        if (isOnSlope && canWalkOnSlope && inputValue == 0f && isGrounded && !isJumping)
        {
            myRigidBody2D.sharedMaterial = fullFriction;
        }
        else
        {
            myRigidBody2D.sharedMaterial = noFriction;
        }
    }

    public bool IsWallSliding()
    {
        bool hitHead = Physics2D.OverlapBox(wallCheckHead.position, wallCheckSize, 0f, wallLayer) != null;
        bool hitFoot = Physics2D.OverlapBox(wallCheckFoot.position, wallCheckSize, 0f, wallLayer) != null;

        return hitHead && hitFoot;
    }

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

    private void ApplyMovement()
    {
        if (isWalled)
        {
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
                myRigidBody2D.linearVelocity = new Vector2(mSpeed * inputValue, myRigidBody2D.linearVelocityY);
            }
        }
        else if (!isGrounded)
        {
            myRigidBody2D.linearVelocity = new Vector2(mSpeed * inputValue, myRigidBody2D.linearVelocityY);
            StopParticles(walkParticle);
        }
    }

    private void CheckFlip()
    {
        if (rockThrow.inThrowState)
        {
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

        if (isWallJumping)
        {
            return;
        }

        if (inputValue * myTransform.right.x < 0f)
        {
            myTransform.right = -myTransform.right;
        }
    }

    private void UpdateAnimations()
    {
        bool isActuallyWallSliding = isTouchingWallAnim && !isGrounded;
        bool isMoving = Mathf.Abs(inputValue) > 0f || Mathf.Abs(myRigidBody2D.linearVelocityX) > 0.5f;

        myAnimator.SetBool("IsWallSliding", isActuallyWallSliding && !isKnockBacked);
        myAnimator.SetBool("IsRunning", isMoving && isGrounded && !isKnockBacked);
        myAnimator.SetBool("IsGrounded", isGrounded && !isKnockBacked);

        bool jumping = !isGrounded && !isActuallyWallSliding && myRigidBody2D.linearVelocityY > 0.5f && !isOnSlope;
        bool falling = !isGrounded && !isActuallyWallSliding && myRigidBody2D.linearVelocityY < -0.1f;

        if (!isKnockBacked)
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

    #region KNOCKED_STAGE

    public void StartKnockedStage()
    {
        if (knockedCoroutine == null)
        {
            knockedCoroutine = StartCoroutine(KnockedStage());
        }
    }

    private IEnumerator KnockedStage()
    {
        isKnocked = true;
        PlayParticles(myOtherParticleSystem);

        if (walkParticle.activeSelf)
        {
            StopParticles(walkParticle);
        }

        groundCheck.gameObject.SetActive(false);
        transform.rotation = Quaternion.Euler(0, 0, 90);

        myRigidBody2D.sharedMaterial.bounciness = 1f;

        yield return new WaitForSeconds(knockedTimer);

        isKnocked = false;
        StopParticles(myOtherParticleSystem);

        groundCheck.gameObject.SetActive(true);
        transform.rotation = Quaternion.Euler(0, 0, 0);

        myRigidBody2D.sharedMaterial.bounciness = 0f;
        knockedCoroutine = null;
    }

    #endregion

    private void FixedUpdate()
    {
        if (!canMove || isKnocked)
        {
            if (!canMove)
            {
                myRigidBody2D.linearVelocity = Vector2.zero;
            }
            return;
        }

        myAnimator.SetBool("IsDashing", isDashing && !isKnockBacked);

        if (isDashing)
        {
            if (IsWallSliding())
            {
                if (dashCoroutine != null)
                {
                    StopCoroutine(dashCoroutine);
                }

                isDashing = false;
                myTrailRenderer.emitting = false;
                ResetGravity();
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

        if (rockThrow.inThrowState)
        {
            if (fastFall)
            {
                ResetGravity();
            }

            if (myRigidBody2D.linearVelocityY > 0f)
            {
                myRigidBody2D.linearVelocity = new Vector2(myRigidBody2D.linearVelocityX, 0f);
            }

            float holdFallGravity = minHoldFallGravity * Mathf.Pow(holdFallGrowthRate, rockThrow.holdTime);
            gravToApply = Mathf.Min(holdFallGravity, playerGravity);
            float clampedFallSpeed = Mathf.Max(myRigidBody2D.linearVelocityY, -maxHoldFallSpeed);
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
                    float slidingVelocityX = Mathf.MoveTowards(myRigidBody2D.linearVelocityX, 0f, groundKnockbackDeceleration * Time.fixedDeltaTime);
                    myRigidBody2D.linearVelocity = new Vector2(slidingVelocityX, myRigidBody2D.linearVelocityY);

                    if (Mathf.Abs(slidingVelocityX) < 0.05f && canCancel)
                    {
                        transform.rotation = Quaternion.Euler(0, 0, 0);
                        isKnockBacked = false;
                        canCancel = false;
                    }
                }

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
    }

    private void Update()
    {
        if (!canMove)
        {
            return;
        }

        UpdateAnimations();
        CheckFlip();
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

        if (groundCheck != null)
        {
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

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