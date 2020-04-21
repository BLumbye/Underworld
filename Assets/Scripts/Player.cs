using Cinemachine;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Controller2D), typeof(SpriteRenderer), typeof(Animator))]
public class Player : MonoBehaviour {
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
    private float velocityXSmoothing;
    private float velocityYSmoothing;
    private bool jumping = false;
    private bool doubleJumped = false;

    // Wall variables
    private int wallDirX;
    private bool wallSliding = false;
    private bool wallGrabbing = false;
    private float wallJumpTime = -1f;
    private int wallJumpDir;
    private float wallClimbJumpTime = -1f;
    private float wallClimbJumpDuration;

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
    private bool wallJumping { get => wallJumpTime + wallJumpReturnDelay > Time.time; }
    private bool wallClimbJumping { get => wallClimbJumpTime + wallClimbJumpDuration > Time.time; }

    // Start is called before the first frame update
    void Start() {
        // Set references
        controller = GetComponent<Controller2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        inventory = Inventory.Instance;

        // Calculate gravity and jump velocity
        gravity = -(2 * maxJumpHeight) / (jumpTime * jumpTime);
        maxJumpVelocity = Mathf.Abs(gravity) * jumpTime;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        // Calculate wall climb jump duration
        wallClimbJumpDuration = (wallClimbJumpForce - wallClimbSpeed) / gravity;
    }

    // Update is called once per frame
    void Update() {
        CalculateVelocity();
        HandleWallSliding();
        HandleWallGrabbing();
        HandleFlipping();

        // Jump from the jump buffer
        if (jumpPressedTime + jumpBufferTime > Time.time && controller.collisions.below) {
            Jump();
        }

        groundedBeforeMovement = controller.collisions.below;

        controller.Move(velocity * Time.deltaTime);

        // Reset velocity if ceiling or ground is hit
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;

        if (velocity.x > 0 && controller.collisions.right)
            velocity.x = 0;

        if (velocity.x < 0 && controller.collisions.left)
            velocity.x = 0;

        if (jumping && velocity.y <= 0)
            jumping = false;

        if (controller.collisions.below)
            doubleJumped = false;

        HandleAnimations();
        HandleCoyoteTimers();
    }

    void CalculateVelocity() {
        float targetVelocityX = directionalInput.x * moveSpeed;
        // If jumping off a wall then limit the movement towards the wall
        if (wallJumping && Mathf.Sign(targetVelocityX) != wallJumpDir)
            targetVelocityX = 0;

        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne);
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

            // Reverse applied gravity
            velocity.y -= gravity * Time.deltaTime;

            // Apply velocity from climbing
            float targetVelocityY = directionalInput.y * wallClimbSpeed;
            velocity.x = 0;
            velocity.y = Mathf.SmoothDamp(velocity.y, targetVelocityY, ref velocityYSmoothing,
                accelerationTimeClimbing);
        }
    }

    void HandleFlipping() {
        if (Mathf.Sign(transform.localScale.x) != controller.collisions.faceDir)
            transform.localScale *= new Vector2(-1, 1);
    }

    void HandleAnimations() {
        if (Mathf.Abs(velocity.x) > 0.5) animator.SetBool("Walking", true);
        else animator.SetBool("Walking", false);

        if (velocity.y > 0 && !wallSliding) animator.SetBool("Jumping", true);
        else animator.SetBool("Jumping", false);

        if (velocity.y < 0 && !wallSliding) animator.SetBool("Falling", true);
        else animator.SetBool("Falling", false);

        if (wallSliding) animator.SetBool("Wall Sliding", true);
        else animator.SetBool("Wall Sliding", false);
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

    void Jump() {
        velocity.y = jumpPressed ? maxJumpVelocity : minJumpVelocity;
        jumping = true;
    }

    void WallJump() {
        if (!(wallSliding || wallGrabbing))
            wallDirX = coyoteWallDirX;

        velocity.x = -wallDirX * wallJumpForce.x;
        velocity.y = wallJumpForce.y;
        wallJumpTime = Time.time;
        wallJumpDir = -wallDirX;
    }

    void WallClimbJump() {
        velocity.y = wallClimbJumpForce;
        wallClimbJumpTime = Time.time;
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
