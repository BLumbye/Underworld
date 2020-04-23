using System;
using Cinemachine;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour {
    [Header("General")]
    [SerializeField] private GameObject spriteObject;

    [Header("Movement")]
    [Tooltip("The maximum jump height in units."), Min(0f)]
    [SerializeField] private float maxJumpHeight = 4;
    [Tooltip("The minimum jump height in units."), Min(0f)]
    [SerializeField] private float minJumpHeight = 1;
    [Tooltip("The time to reach jump apex (Max Jump Height)."), Min(0f)]
    [SerializeField] private float jumpTime = 0.4f;
    [Tooltip("The time to reach max speed when grounded."), Min(0f)]
    [SerializeField] private float accelerationTimeGrounded = 0.1f;
    [Tooltip("The time to reach max speed when airborne."), Min(0f)]
    [SerializeField] private float accelerationTimeAirborne = 0.2f;
    [Tooltip("The maximum move speed."), Min(0f)]
    [SerializeField] private float moveSpeed = 6;
    [Tooltip("The time you are still able to jump after falling of a platform or wall."), Min(0f)]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("The time you can press the jump button and still jump, before reaching the ground."), Min(0f)]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Wall Jump")]
    [Tooltip("The force applied when jumping while facing towards the wall.")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(7.5f, 16f);
    [Tooltip("The time before you can move back towards the wall.")]
    [SerializeField] private float wallJumpReturnDelay = 0.7f;
    //[Tooltip("The force applied when jumping whilst no directional input is received.")]
    //[SerializeField] private Vector2 wallJumpOff = new Vector2(8.5f, 7f);
    //[Tooltip("The force applied when jumping while facing away from the wall.")]
    //[SerializeField] private Vector2 wallLeap = new Vector2(18f, 17f);
    [Label("Maximum Wall Slide Speed"), Tooltip("The maximum speed when sliding. Sliding is cancelled whilst pressing down."), Min(0f)]
    [SerializeField] private float wallSlideSpeedMax = 3;

    [Header("Wall Climb")]
    [Tooltip("The speed you go up and down when wall climbing."), Min(0f)]
    [SerializeField] private float wallClimbSpeed = 2;
    [Tooltip("The time to reach max climbing speed."), Min(0f)]
    [SerializeField] private float accelerationTimeClimbing = 0.1f;
    [Tooltip("The force, in the y-direction, that is applied when jump up against wall whilst climbing."), Min(0f)]
    [SerializeField] private float wallClimbJumpForce = 10f;

    [Header("Dash")]
    [SerializeField] private float dashLength = 3;
    [SerializeField] private float dashTime = 0.3f;
    [SerializeField] private float dashCooldown = 0.1f;

    [Header("Grappling Hook")]
    [SerializeField] private float grappleRange = 10f;
    [SerializeField] private LayerMask grapplePointLayer;
    [SerializeField] private GameObject grappleCrosshair;
    [SerializeField] private LineRenderer grappleLine;
    [SerializeField] private float grappleSpeed = 4f;
    [SerializeField] private float shootTime = 0.3f;


    // Coyote variables
    private float coyoteJumpTime = -1f;
    private float coyoteWallslideTime = -1f;
    private int coyoteWallDirX = 1;
    private bool groundedBeforeMovement = false;

    // Jump buffer - if jump is pressed before reaching ground
    private float jumpPressedTime = -1f;

    // Movement variables
    private Vector2 velocity;
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector2 velocitySmoothing;
    private bool jumping = false;
    private bool doubleJumped = false;

    // Wall variables
    private int wallDirX;
    private bool wallSliding = false;
    private bool wallGrabbing = false;
    private float wallJumpTimestamp = -1f;
    private int wallJumpDir;
    private float wallClimbJumpTimestamp = -1f;
    private float wallClimbJumpDuration;

    // Dash variables
    private float dashVelocity;
    private float dashStartedTimestamp = -1f;
    private float dashFinishedTimestamp = -1f;
    private bool dashed = false;
    private bool dashing = false;
    private int dashingDirection;

    // Grapple variables
    private Transform grappleTarget;
    private Transform currentGrapplePoint;
    private bool grappling;
    private float grappleExtendProgess = 0f;

    // References
    private Controller2D controller;
    private SpriteRenderer sr;
    private Animator animator;
    private Inventory inventory;

    // Input variables
    private Vector2 directionalInput;
    private bool downPressed = false;
    private bool jumpPressed = false;
    private bool wallClimbPressed = false;

    // Computed variables
    private bool wallJumping { get => wallJumpTimestamp + wallJumpReturnDelay > Time.time; }
    private bool wallClimbJumping { get => wallClimbJumpTimestamp + wallClimbJumpDuration > Time.time; }

    // Start is called before the first frame update
    void Start() {
        // Set references
        controller = GetComponent<Controller2D>();
        sr = spriteObject.GetComponent<SpriteRenderer>();
        animator = spriteObject.GetComponent<Animator>();
        inventory = Inventory.Instance;

        // Calculate gravity and jump velocity
        gravity = -(2 * maxJumpHeight) / (jumpTime * jumpTime);
        maxJumpVelocity = Mathf.Abs(gravity) * jumpTime;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        // Calculate wall climb jump duration
        wallClimbJumpDuration = (wallClimbJumpForce - wallClimbSpeed) / gravity;

        // Calculate dash velocity
        dashVelocity = dashLength / dashTime;
    }

    // Update is called once per frame
    void Update() {
        CalculateVelocity();
        HandleWallSliding();
        HandleWallGrabbing();
        HandleFlipping();
        HandleDashing();
        HandleGrappling();

        // Jump from the jump buffer
        if (jumpPressedTime + jumpBufferTime > Time.time && controller.collisions.below) {
            Jump();
        }

        groundedBeforeMovement = controller.collisions.below;

        if (!(grappling && grappleExtendProgess >= 1)) controller.Move(velocity * Time.deltaTime);

        // Reset velocity if ceiling or ground is hit
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;

        if (velocity.x > 0 && controller.collisions.right)
            velocity.x = 0;

        if (velocity.x < 0 && controller.collisions.left)
            velocity.x = 0;

        if (jumping && velocity.y <= 0)
            jumping = false;

        if (controller.collisions.below) {
            dashed = false;
            doubleJumped = false;
        }

        HandleAnimations();
        HandleCoyoteTimers();
    }

    void CalculateVelocity() {
        // Don't do this is wall grabbing or grappling
        if (wallGrabbing || (grappling && grappleExtendProgess >= 1))
            return;

        float targetVelocityX = directionalInput.x * moveSpeed;
        // If jumping off a wall then limit the movement towards the wall
        if (wallJumping && Mathf.Sign(targetVelocityX) != wallJumpDir)
            targetVelocityX = 0;

        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocitySmoothing.x, controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }

    void HandleWallSliding() {
        if (!inventory.HasRelic("Pickaxe"))
            return;

        wallDirX = controller.collisions.left ? -1 : 1;
        wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below &&
            velocity.y < 0 && !downPressed && !wallClimbJumping) {
            wallSliding = true;
            jumping = false;

            if (velocity.y < -wallSlideSpeedMax) {
                velocity.y = -wallSlideSpeedMax;
            }
        }
    }

    void HandleWallGrabbing() {
        if (!inventory.HasRelic("Sharpened Pickaxe"))
            return;

        wallDirX = controller.collisions.left ? -1 : 1;
        wallGrabbing = false;
        if (wallClimbPressed && (controller.collisions.left || controller.collisions.right) && !wallJumping && !wallClimbJumping) {
            wallSliding = false;
            wallGrabbing = true;
            jumping = false;

            // Apply velocity from climbing
            float targetVelocityY = directionalInput.y * wallClimbSpeed;
            velocity.x = 0;
            velocity.y = Mathf.SmoothDamp(velocity.y, targetVelocityY, ref velocitySmoothing.y,
                accelerationTimeClimbing);
        }
    }

    void HandleFlipping() {
        sr.flipX = controller.collisions.faceDir == -1;
    }

    void HandleAnimations() {
        bool grounded = controller.collisions.below;
        float walk_speed = Mathf.Abs(velocity.x) / moveSpeed;
        bool running = grounded && walk_speed > 0.25f;
        bool walking = grounded && walk_speed > 0.01f && !running;
        bool idle = !walking && !running;

        animator.SetBool("grounded", grounded);
        animator.SetBool("walking", walking);
        animator.SetBool("running", running);
        animator.SetBool("idle", idle);
        animator.SetFloat("walk_speed", walk_speed);
    }

    void HandleCoyoteTimers() {
        if (groundedBeforeMovement && !controller.collisions.below && velocity.y < 0) {
            coyoteJumpTime = Time.time;
        }

        if (!((controller.collisions.left || controller.collisions.right) && !controller.collisions.below &&
              velocity.y < 0) && wallSliding) {
            coyoteWallslideTime = Time.time;
            coyoteWallDirX = wallDirX;
        }
    }

    void HandleDashing() {
        if (!dashing)
            return;

        velocity.y = 0;
        velocity.x = dashVelocity * dashingDirection;

        wallDirX = controller.collisions.left ? -1 : 1;
        if (dashStartedTimestamp + dashTime <= Time.time ||
            (controller.collisions.left || controller.collisions.right) && wallDirX == dashingDirection) {
            dashing = false;
            dashFinishedTimestamp = Time.time;
        }
    }

    void HandleGrappling() {
        if (!inventory.HasRelic("Grappling Hook"))
            return;

        // Target the closest grapple point that's in front and above the player
        Collider2D[] hit = Physics2D.OverlapCircleAll(transform.position, grappleRange, grapplePointLayer);
        Collider2D closestPoint = null;
        float closestDistance = Mathf.Infinity;
        foreach (Collider2D point in hit) {
            // Check if the point is in the in front and above the player - with a margin of 2 units
            if (point.transform.position.y + 2 > transform.position.y &&
                (Math.Sign(point.transform.position.x - transform.position.x) == controller.collisions.faceDir || Mathf.Abs(point.transform.position.x - transform.position.x) <= 2) &&
                point.transform != currentGrapplePoint) {
                // Check line of sight
                float distance = Vector2.Distance(point.transform.position, transform.position);
                RaycastHit2D lineHit = Physics2D.Raycast(transform.position, point.transform.position - transform.position,
                    distance, controller.collisionMask);
                
                if (distance < closestDistance && !lineHit) {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }
        }

        if (closestPoint == null && grappleTarget != null) {
            // Hide the crosshair
            grappleCrosshair.SetActive(false);
            grappleTarget = null;
        } else if (closestPoint != grappleTarget) {
            // Show crosshair
            grappleCrosshair.transform.position = closestPoint.transform.position;
            grappleCrosshair.SetActive(true);

            grappleTarget = closestPoint.transform;
        }

        if (grappling && grappleExtendProgess < 1) {
            grappleExtendProgess += Time.deltaTime / shootTime;
            grappleLine.SetPositions(new Vector3[] {
                transform.position, transform.position + (currentGrapplePoint.position - transform.position) * grappleExtendProgess
            });
        } else if (grappling) {
            Vector2 ropeVector = currentGrapplePoint.position - transform.position;
            float distance = ropeVector.magnitude;
            if (distance > grappleRange) {
                ResetGrapple();
                return;
            }

            grappleLine.SetPositions(new Vector3[] {
                transform.position, currentGrapplePoint.position
            });

            // Apply gravity and calculate velocity
            if (distance > 0.1) {
                velocity.y += gravity * Time.deltaTime;
                Vector2 perpendicular = Vector2.Perpendicular(currentGrapplePoint.position - transform.position);
                velocity = Vector2.Dot(velocity, perpendicular) / perpendicular.sqrMagnitude * perpendicular * (1 - 0.9f * Time.deltaTime);
            } else {
                velocity = Vector2.zero;
                dashed = false;
                doubleJumped = false;
                if (Mathf.Abs(directionalInput.x) > 0.5)
                    controller.collisions.faceDir = Math.Sign(directionalInput.x);
            }

            // Move - but if were very close, then just go straight to the point without velocity
            controller.Move(distance < 0.1 ? ropeVector : velocity * Time.deltaTime + ropeVector.normalized * grappleSpeed * Time.deltaTime);
        }
    }

    void Jump() {
        velocity.y = jumpPressed ? maxJumpVelocity : minJumpVelocity;
        jumping = true;
        ResetGrapple();
    }

    void WallJump() {
        if (!(wallSliding || wallGrabbing))
            wallDirX = coyoteWallDirX;

        velocity.x = -wallDirX * wallJumpForce.x;
        velocity.y = wallJumpForce.y;
        wallJumpTimestamp = Time.time;
        wallJumpDir = -wallDirX;
    }

    void WallClimbJump() {
        velocity.y = wallClimbJumpForce;
        wallClimbJumpTimestamp = Time.time;
    }

    void Dash() {
        if (dashed || !inventory.HasRelic("Boosted Rocket Boots") || dashFinishedTimestamp + dashCooldown > Time.time)
            return;

        dashing = true;
        dashed = true;
        dashingDirection = Math.Sign(directionalInput.x);
        dashStartedTimestamp = Time.time;
        ResetGrapple();
    }

    void Grapple() {
        if (!inventory.HasRelic("Grappling Hook"))
            return;

        if (grappleTarget) {
            grappleExtendProgess = 0f;
            grappling = true;
            grappleLine.enabled = true;
            currentGrapplePoint = grappleTarget;
        } else {
            ResetGrapple();
        }
    }

    void ResetGrapple() {
        grappling = false;
        grappleLine.enabled = false;
        currentGrapplePoint = null;
    }

    public void OnJumpPressed() {
        if (downPressed && controller.collisions.standingOnHollow) {
            // If holding down and standing on a hollow platform fall through instead
            controller.FallThroughPlatform();
        } if ((wallSliding || coyoteWallslideTime + coyoteTime > Time.time) && inventory.HasRelic("Pickaxe") ||
              wallGrabbing && Mathf.Abs(directionalInput.x) >= 0.5f && Mathf.Sign(directionalInput.x) != wallDirX && inventory.HasRelic("Sharpened Pickaxe")) {
            // Jump off wall
            WallJump();
        } else if (wallGrabbing && inventory.HasRelic("Sharpened Pickaxe")) {
            // Jump up the wall
            WallClimbJump();
        } else if (controller.collisions.below || coyoteJumpTime + coyoteTime > Time.time) {
            Jump();
        } else if (!doubleJumped && inventory.HasRelic("Rocket Boots")) {
            doubleJumped = true;
            Jump();
        } else {
            jumpPressedTime = Time.time;
        }

        // Reset coyote timers
        coyoteJumpTime = -1f;
        coyoteWallslideTime = -1f;
    }

    public void OnJumpReleased() {
        // Cancel jump if releasing the jump button
        // BUG: If something pushes you up and you press jump, you can reset your upwards velocity
        if (velocity.y > minJumpVelocity && jumping) {
            velocity.y = minJumpVelocity;
        }
    }

    public void OnJumpInput(InputAction.CallbackContext ctx) {
        if (!ctx.performed)
            return;

        jumpPressed = ctx.ReadValue<float>() >= 0.5f;
        if (jumpPressed) OnJumpPressed();
        else OnJumpReleased();
    }

    public void OnDashInput(InputAction.CallbackContext ctx) {
        if (!ctx.performed)
            return;

        Dash();
    }

    public void OnGrappleInput(InputAction.CallbackContext ctx) {
        if (!ctx.performed)
            return;

        Grapple();
    }

    public void OnDownInput(InputAction.CallbackContext ctx) {
        downPressed = ctx.ReadValue<float>() >= 0.5f;
    }

    public void OnWallClimbInput(InputAction.CallbackContext ctx) {
        wallClimbPressed = ctx.ReadValue<float>() >= 0.25f;
    }

    public void SetDirectionalInput(InputAction.CallbackContext ctx) {
        directionalInput = ctx.ReadValue<Vector2>();
    }
}
