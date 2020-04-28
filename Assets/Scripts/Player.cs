using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour {
    #region Inspector variables
    [Header("General")]
    [SerializeField] private GameObject spriteObject;
    [SerializeField] private GameObject spriteScaleObject;
    [SerializeField] private int maxHealth = 3;

    [Header("Hands")]
    [SerializeField] private GameObject handTargetContainer;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject leftHandTarget;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject rightHandTarget;
    [SerializeField] private float handSmoothing = 50f;
    [SerializeField] private Vector2 grappleHandCenter = Vector2.zero;
    [SerializeField] private float grappleHandRadius = 0.5f;

    [Header("UI")]
    [SerializeField] private GameObject heartContainer;
    [SerializeField] private GameObject heartObject;
    [SerializeField] private Sprite activeHeartSprite;
    [SerializeField] private Sprite inactiveHeartSprite;
    [SerializeField] private Volume postProcessingVolume;

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
    [Label("Maximum Wall Slide Speed"), Tooltip("The maximum speed when sliding. Sliding is cancelled whilst pressing down."), Min(0f)]
    [SerializeField] private float wallSlideSpeedMax = 3;

    [Header("Wall Climb")]
    [Tooltip("The speed you go up and down when wall climbing."), Min(0f)]
    [SerializeField] private float wallClimbSpeed = 2;
    [Tooltip("The time to reach max climbing speed."), Min(0f)]
    [SerializeField] private float accelerationTimeClimbing = 0.1f;
    [Tooltip("The force, in the y-direction, that is applied when jump up against wall whilst climbing."), Min(0f)]
    [SerializeField] private float wallClimbJumpForce = 10f;
    [SerializeField] private float wallClimbJumpCooldown = 0.3f;

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
    [SerializeField] private float hangingDistance = 0.6f;

    [Header("Particles")]
    [SerializeField] ParticleSystem jumpParticle;
    [SerializeField] ParticleSystem wallSlideParticle;
    [SerializeField] ParticleSystem wallClimbParticle;
    [SerializeField] ParticleSystem wallJumpParticle;
    [SerializeField] ParticleSystem dashParticle;
    [SerializeField] ParticleSystem dashGhostParticle;
    #endregion

    #region Private variables
    #region Hands
    private SpriteRenderer leftHandSR;
    private SpriteRenderer rightHandSR;
    #endregion

    #region Hearts
    private int health;
    private List<Image> heartObjects = new List<Image>();
    private Vignette vignette;
    private float defaultVignetteIntensity;
    private Color defaultVignetteColor;
    #endregion

    #region Coyote
    private float coyoteJumpTime = -1f;
    private float coyoteWallslideTime = -1f;
    private int coyoteWallDirX = 1;
    private bool groundedBeforeMovement = false;
    #endregion

    #region Jump buffer
    private float jumpPressedTime = -1f;
    #endregion

    #region Movement
    private Vector2 velocity;
    public float gravity { private set; get; }
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector2 velocitySmoothing;
    private bool jumping = false;
    private bool doubleJumped = false;
    #endregion

    #region Wall sliding & climb
    private int wallDirX;
    private bool wallSliding = false;
    private bool wallGrabbing = false;
    private float wallJumpTimestamp = -1f;
    private int wallJumpDir;
    private float wallClimbJumpTimestamp = -1f;
    private float wallClimbJumpDuration;
    #endregion

    #region Dash
    private float dashVelocity;
    private float dashStartedTimestamp = -1f;
    private float dashFinishedTimestamp = -1f;
    private bool dashed = false;
    private bool dashing = false;
    private int dashingDirection;
    #endregion

    #region Grapple
    private Transform grappleTarget;
    private Transform currentGrapplePoint;
    private bool grappling;
    private float grappleExtendProgess = 0f;
    private Vector2 grappleVelocity;
    private Vector3 grappleOrigin;
    #endregion

    #region References
    private Controller2D controller;
    private SpriteRenderer sr;
    private Animator animator;
    private Inventory inventory;
    private CameraController camera;
    #endregion

    #region Input
    private Vector2 directionalInput;
    private bool downPressed = false;
    private bool jumpPressed = false;
    private bool wallClimbPressed = false;
    #endregion
    #endregion

    #region Computed variables
    private bool wallJumping { get => wallJumpTimestamp + wallJumpReturnDelay > Time.time; }
    private bool wallClimbJumping { get => wallClimbJumpTimestamp + wallClimbJumpDuration > Time.time; }
    private Vector2 ropeVector {
        get =>
            (Vector2) (currentGrapplePoint.position - transform.position) - grappleHandCenter -
            new Vector2(0, grappleHandRadius + hangingDistance);
    }
    #endregion

    // Start is called before the first frame update
    void Start() {
        // Set references
        controller = GetComponent<Controller2D>();
        sr = spriteObject.GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        inventory = Inventory.Instance;
        camera = Camera.main.GetComponent<CameraController>();

        // Calculate gravity and jump velocity
        gravity = -(2 * maxJumpHeight) / (jumpTime * jumpTime);
        maxJumpVelocity = Mathf.Abs(gravity) * jumpTime;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        // Calculate wall climb jump duration
        wallClimbJumpDuration = (wallClimbJumpForce - wallClimbSpeed) / -gravity;

        // Calculate dash velocity
        dashVelocity = dashLength / dashTime;

        // Get hand renderers
        leftHandSR = leftHand.GetComponent<SpriteRenderer>();
        rightHandSR = rightHand.GetComponent<SpriteRenderer>();

        // Health
        health = maxHealth;
        InitializeHearts();

        DOTween.Init();
    }

    // Update is called once per frame
    void Update() {
        CalculateVelocity();
        HandleWallSliding();
        HandleWallGrabbing();
        HandleFlipping();
        HandleDashing();
        HandleGrappling();

        // If paused stop velocity
        if (Inventory.Instance.paused) directionalInput = Vector2.zero;

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

        // Reset dash and double jump if on ground
        if (controller.collisions.below) {
            dashed = false;
            doubleJumped = false;
        }

        // Play jump particle if we hit ground
        if (!groundedBeforeMovement && controller.collisions.below) {
            PlayJumpParticle();
        }

        HandleAnimations();
        HandleCoyoteTimers();
        UpdateHands();
    }

    void InitializeHearts() {
        for (int i = 0; i < maxHealth; i++) {
            heartObjects.Add(Instantiate(heartObject, heartContainer.transform).GetComponent<Image>());
        }

        postProcessingVolume.profile.TryGet(out vignette);
        defaultVignetteIntensity = vignette.intensity.value;
        defaultVignetteColor = vignette.color.value;
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
        if (sr.flipX != (controller.collisions.faceDir == -1)) {
            sr.flipX = controller.collisions.faceDir == -1;

            // Flip particles
            jumpParticle.transform.localScale = new Vector3(controller.collisions.faceDir, 1);
            wallSlideParticle.transform.localScale = new Vector3(controller.collisions.faceDir, 1);
            wallClimbParticle.transform.localScale = new Vector3(controller.collisions.faceDir, 1);
            dashParticle.transform.localScale = new Vector3(controller.collisions.faceDir, 1);
            dashGhostParticle.transform.localScale = new Vector3(controller.collisions.faceDir, 1);
            if (Math.Sign(wallSlideParticle.transform.localPosition.x) != controller.collisions.faceDir) {
                wallSlideParticle.transform.localPosition *= new Vector2(-1, 1);
                wallClimbParticle.transform.localPosition *= new Vector2(-1, 1);
                wallJumpParticle.transform.localPosition *= new Vector2(-1, 1);
                dashParticle.transform.localPosition *= new Vector2(-1, 1);
                dashGhostParticle.transform.localPosition *= new Vector2(-1, 1);
            }

            if (controller.collisions.below)
                PlayJumpParticle();
        }

        wallJumpParticle.transform.localScale = new Vector3((wallSliding || wallGrabbing) ? wallDirX : coyoteWallDirX, 1);
    }

    void UpdateHands() {
        // Set the sorting order of hands
        leftHandSR.sortingOrder = controller.collisions.faceDir * -2;
        rightHandSR.sortingOrder = controller.collisions.faceDir * 2;

        // Move hands
        leftHand.transform.position = Vector2.Lerp(leftHand.transform.position, leftHandTarget.transform.position,  grappling ? 1 : handSmoothing * Time.deltaTime);
        rightHand.transform.position = Vector2.Lerp(rightHand.transform.position, rightHandTarget.transform.position, grappling ? 1 : handSmoothing * Time.deltaTime);
    }

    void HandleAnimations() {
        bool grounded = controller.collisions.below;
        bool right = controller.collisions.faceDir == 1;
        float walk_speed = Mathf.Abs(velocity.x) / moveSpeed;
        bool running = grounded && walk_speed > 0.25f;
        bool walking = grounded && walk_speed > 0.01f && !running;
        bool wall_slide = (wallSliding || wallGrabbing && velocity.y <= 0.5f) && !grounded;
        bool wall_climb = wallGrabbing && velocity.y > 0.5f && !grounded && !wallClimbJumping;
        bool idle = !walking && !running && grounded;
        bool jumping = !grounded && velocity.y > 0 && !wall_slide && !wall_climb || wallClimbJumping || grappling;
        bool falling = !grounded && !jumping && !wall_slide && !wall_climb && !grappling;

        // Set variables in the animator
        animator.SetBool("grounded", grounded);
        animator.SetBool("walking", walking);
        animator.SetBool("running_left", running && !right);
        animator.SetBool("running_right", running && right);
        animator.SetBool("idle", idle);
        animator.SetBool("jumping", jumping);
        animator.SetBool("falling", falling);
        animator.SetBool("wall_slide_left", wall_slide && !right);
        animator.SetBool("wall_slide_right", wall_slide && right);
        animator.SetBool("wall_climb_left", wall_climb && !right);
        animator.SetBool("wall_climb_right", wall_climb && right);

        animator.SetFloat("walk_speed", walk_speed);

        // Set the scale if in the jumping or falling animation
        if ((jumping || falling) && !grappling) {
            spriteScaleObject.transform.localScale = new Vector3(1 - Mathf.Abs(velocity.y) / 300, 1 + Mathf.Abs(velocity.y) / 300, 1);
        } else {
            spriteScaleObject.transform.localScale = new Vector3(1, 1, 1);
        }

        // Grappling animations are programmed as they are way too dynamic for the built-in animation system
        if (grappling) {
            // Make the hands hang out from the player towards the grapple target, but perpendicular from the rope besides each other
            Vector2 perpendicular = Vector2.Perpendicular(currentGrapplePoint.position - transform.position + (Vector3) grappleHandCenter).normalized;
            leftHandTarget.transform.position = grappleOrigin + (Vector3) perpendicular * -0.06f; 
            rightHandTarget.transform.position = grappleOrigin + (Vector3)perpendicular * 0.06f;
        }

        // Play some particle effects
        if (wall_slide && !wallSlideParticle.isPlaying) {
            wallSlideParticle.Play();
        } else if (!wall_slide && wallSlideParticle.isPlaying) {
            wallSlideParticle.Stop();
        }
        if (wall_climb && !wallClimbParticle.isPlaying) {
            wallClimbParticle.Play();
        } else if (!wall_climb && wallClimbParticle.isPlaying) {
            wallClimbParticle.Stop();
        }
        if (dashing && !dashParticle.isPlaying) {
            dashParticle.Play();
            dashGhostParticle.Play();
        } else if (!dashing && dashParticle.isPlaying) {
            dashParticle.Stop();
            dashGhostParticle.Stop();
        }
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

        Vector2 grappleCenter = grappleHandCenter + (Vector2)transform.position;

        // Target the closest grapple point that's in front and above the player
        // If none is in front and above, try to find one behind the player within a margin of 2 units
        Collider2D[] hit = Physics2D.OverlapCircleAll(grappleCenter, grappleRange, grapplePointLayer);
        Collider2D closestPoint = null;
        float closestDistance = Mathf.Infinity;
        Collider2D closestPointBehind = null;
        float closestDistanceBehind = Mathf.Infinity;
        foreach (Collider2D point in hit) {
            // Check if the point is in the in front and above the player
            int targetingDirection = Mathf.Abs(directionalInput.x) > 0.25f ? Math.Sign(directionalInput.x) : controller.collisions.faceDir;
            int pointDirection = Math.Sign(point.transform.position.x - grappleCenter.x);

            // Check line of sight
            float distance = Vector2.Distance(point.transform.position, grappleCenter);
            RaycastHit2D lineHit = Physics2D.Raycast(grappleCenter, point.transform.position - transform.position,
                distance, controller.collisionMask);

            // Check if it fullfills all requirements and is the closest
            if (point.transform.position.y > grappleCenter.y &&
                pointDirection == targetingDirection &&
                point.transform != currentGrapplePoint &&
                !lineHit && distance < closestDistance) {
                closestDistance = distance;
                closestPoint = point;
            } else if (point.transform.position.y + 2 > grappleCenter.y &&
                       (pointDirection == targetingDirection ||
                        Mathf.Abs(point.transform.position.x - grappleCenter.x) <= 2) &&
                       point.transform != currentGrapplePoint &&
                       !lineHit && distance < closestDistanceBehind) {
                closestDistanceBehind = distance;
                closestPointBehind = point;
            }
        }

        // If no point is in front use a point behind
        if (closestPoint == null && closestPointBehind != null) {
            closestPoint = closestPointBehind;
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

        if (grappling) {
            grappleOrigin = (currentGrapplePoint.position - (Vector3) grappleCenter).normalized * grappleHandRadius + (Vector3) grappleHandCenter + transform.position;

            if (grappleExtendProgess < 1) {
                grappleExtendProgess += Time.deltaTime / shootTime;
                if (grappleExtendProgess > 1)
                    grappleExtendProgess = 1;
                grappleLine.SetPositions(new Vector3[] {
                    grappleOrigin, grappleOrigin + (currentGrapplePoint.position - grappleOrigin) * grappleExtendProgess
                });
                return;
            }

            // Offset this because we want to hang from it with the hands above us and a little extra space
            float distance = ropeVector.magnitude;
            if (distance > grappleRange) {
                ResetGrapple();
                return;
            }

            grappleLine.SetPositions(new Vector3[] {
                grappleOrigin, currentGrapplePoint.position
            });

            // Apply gravity and calculate velocity
            if (distance > 0.1) {
                velocity.y += gravity * Time.deltaTime;
                Vector2 perpendicular = Vector2.Perpendicular(currentGrapplePoint.position - transform.position);
                velocity = Vector2.Dot(velocity, perpendicular) / perpendicular.sqrMagnitude * perpendicular * (1 - 0.9f * Time.deltaTime);
                grappleVelocity = ropeVector.normalized * grappleSpeed;
            } else {
                velocity = Vector2.zero;
                grappleVelocity = Vector2.zero;
                dashed = false;
                doubleJumped = false;
                if (Mathf.Abs(directionalInput.x) > 0.5)
                    controller.collisions.faceDir = Math.Sign(directionalInput.x);
            }

            // Move - but if were very close, then just go straight to the point without velocity
            controller.Move(distance < 0.1f ? distance < 0.001f ? Vector2.zero : ropeVector : (velocity + grappleVelocity) * Time.deltaTime);
        }
    }

    void Jump() {
        velocity.y = jumpPressed ? maxJumpVelocity : minJumpVelocity;
        PlayJumpParticle();
        jumping = true;
    }

    void WallJump() {
        if (!(wallSliding || wallGrabbing))
            wallDirX = coyoteWallDirX;

        velocity.x = -wallDirX * wallJumpForce.x;
        velocity.y = wallJumpForce.y;
        wallJumpTimestamp = Time.time;
        wallJumpDir = -wallDirX;
        wallJumpParticle.Play();
    }

    void WallClimbJump() {
        velocity.y = wallClimbJumpForce;
        wallClimbJumpTimestamp = Time.time;
        wallJumpParticle.Play();
    }

    void Dash() {
        if (dashed || !inventory.HasRelic("Boosted Rocket Boots") || dashFinishedTimestamp + dashCooldown > Time.time)
            return;

        dashing = true;
        dashed = true;
        dashingDirection = Mathf.Abs(directionalInput.x) > 0.25f ? Math.Sign(directionalInput.x) : controller.collisions.faceDir;
        dashStartedTimestamp = Time.time;
        ResetGrapple();
    }

    void Grapple() {
        if (!inventory.HasRelic("Grappling Hook"))
            return;

        if (grappleTarget) {
            ResetGrapple();
            grappling = true;
            grappleLine.enabled = true;
            currentGrapplePoint = grappleTarget;
        } else {
            ResetGrapple();
        }
    }

    void ResetGrapple() {
        velocity += grappleVelocity;
        grappling = false;
        grappleLine.enabled = false;
        currentGrapplePoint = null;
        grappleVelocity = Vector2.zero;
        grappleExtendProgess = 0f;
    }

    void PlayJumpParticle() {
        jumpParticle.Play();
    }

    public void Damage() {
        health--;
        if (health <= 0) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        camera.trauma += 0.35f;
        UpdateHearts();
        StartCoroutine(DamageAnimation());
    }

    IEnumerator DamageAnimation() {
        DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, 0.4f, 0.1f);
        DOTween.To(() => vignette.color.value, x => vignette.color.value = x, Color.red, 0.1f);
        yield return new WaitForSeconds(0.175f);
        DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, defaultVignetteIntensity, 1f).SetEase(Ease.OutCubic);
        DOTween.To(() => vignette.color.value, x => vignette.color.value = x, defaultVignetteColor, 1f).SetEase(Ease.OutCubic);
    }

    public void Knockback(Vector2 knockback) {
        velocity += knockback;
        camera.trauma += knockback.magnitude / 200;
    }

    public void UpdateHearts() {
        for (int i = 0; i < maxHealth; i++) {
            if (health >= i + 1 && heartObjects[i].sprite != activeHeartSprite) {
                heartObjects[i].sprite = activeHeartSprite;
            } else if (health < i + 1 && heartObjects[i].sprite != inactiveHeartSprite) {
                heartObjects[i].sprite = inactiveHeartSprite;
            }
        }
    }

    #region Input functions
    public void OnJumpPressed() {
        ResetGrapple();

        if (downPressed && controller.collisions.standingOnHollow) {
            // If holding down and standing on a hollow platform fall through instead
            controller.FallThroughPlatform();
        } if ((wallSliding || coyoteWallslideTime + coyoteTime > Time.time) && inventory.HasRelic("Pickaxe") ||
              wallGrabbing && Mathf.Abs(directionalInput.x) >= 0.5f && Mathf.Sign(directionalInput.x) != wallDirX && inventory.HasRelic("Sharpened Pickaxe")) {
            // Jump off wall
            WallJump();
        } else if (wallGrabbing && inventory.HasRelic("Sharpened Pickaxe") && wallClimbJumpTimestamp + wallClimbJumpDuration + wallClimbJumpCooldown <= Time.time) {
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
        if (!ctx.performed || Inventory.Instance.paused)
            return;

        jumpPressed = ctx.ReadValue<float>() >= 0.5f;
        if (jumpPressed) OnJumpPressed();
        else OnJumpReleased();
    }

    public void OnDashInput(InputAction.CallbackContext ctx) {
        if (!ctx.performed || Inventory.Instance.paused)
            return;

        Dash();
    }

    public void OnGrappleInput(InputAction.CallbackContext ctx) {
        if (!ctx.performed || Inventory.Instance.paused)
            return;

        Grapple();
    }

    public void OnDownInput(InputAction.CallbackContext ctx) {
        if (Inventory.Instance.paused)
            return;

        downPressed = ctx.ReadValue<float>() >= 0.5f;
    }

    public void OnWallClimbInput(InputAction.CallbackContext ctx) {
        if (Inventory.Instance.paused)
            return;

        wallClimbPressed = ctx.ReadValue<float>() >= 0.25f;
    }

    public void SetDirectionalInput(InputAction.CallbackContext ctx) {
        if (Inventory.Instance.paused)
            return;

        directionalInput = ctx.ReadValue<Vector2>();
    }
    #endregion

    private void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere((Vector3) grappleHandCenter + transform.position, grappleHandRadius);
    }
}
