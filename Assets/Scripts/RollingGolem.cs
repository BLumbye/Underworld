using System;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class RollingGolem : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float range = 8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float accelerationTime = 0.5f;
    [SerializeField] private float hitCooldown = 1f;
    [SerializeField] private Vector2 playerKnockback;
    [SerializeField] private string inactiveSortingLayerName = "ProjectilesInactive";
    [SerializeField] private float collisionRadius = 0.3f;

    private Animator animator;
    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;
    private Controller2D controller;
    private Player player;

    private Vector2 velocity;
    private float velocityXSmoothing;
    private float gravity;
    private float hitTime = -1f;
    private float rollingDir = 1;

    private bool awake = false;
    private bool waking = false;
    private bool rolling = false;
    private bool preparingRoll = false;
    private bool stunned = false;
    private bool dying = false;

    void Start() {
        controller = GetComponent<Controller2D>();
        animator = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        sr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_EmissionColor", emissionColor);
        sr.SetPropertyBlock(propertyBlock);

        // Get gravity from the player
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        gravity = player.gravity;
    }

    void Update() {
        if (emissionColor != propertyBlock.GetColor("_EmissionColor")) {
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            sr.SetPropertyBlock(propertyBlock);
        }

        if (dying || stunned) {
            return;
        }

        bool playerInRange = IsPlayerInRange();
        if (playerInRange && !awake && !waking) {
            animator.SetTrigger("wake");
        } else if (playerInRange && awake && !rolling && !preparingRoll && !stunned) {
            rollingDir = player.transform.position.x > transform.position.x ? 1 : -1;
            animator.SetBool("right", rollingDir == 1);
            preparingRoll = true;
            animator.SetTrigger("roll");
        } else if (rolling) {
            float targetVelocityX = rollingDir * speed;
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTime);
            velocity.y += gravity * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);

            // Reset velocity if ceiling or ground is hit
            if (controller.collisions.above || controller.collisions.below)
                velocity.y = 0;

            if (rollingDir == 1 && controller.collisions.right ||
                rollingDir == -1 && controller.collisions.left) {
                velocity.x = 0;
                rolling = false;
                stunned = true;
                animator.SetTrigger("stun");
            }

            CheckCollision();
        }

        // Flip sprite
        if (sr.flipX != (rollingDir == 1))
            sr.flipX = rollingDir == 1;
    }

    void CheckCollision() {
        if (Physics2D.OverlapCircle(sr.transform.position, collisionRadius, playerLayer) && hitTime + hitCooldown <= Time.time) {
            player.Damage();
            player.Knockback(playerKnockback * new Vector2(controller.collisions.faceDir, 1));
            hitTime = Time.time;
        }
    }

    public void WakeFinished() {
        waking = false;
        awake = true;
    }

    public void PreparingRollFinished() {
        preparingRoll = false;
        rolling = true;
    }

    public void StunFinished() {
        stunned = false;
    }

    public void DyingFinished() {
        controller.enabled = false;
        enabled = false;
    }

    public void Kill() {
        dying = true;
        sr.sortingLayerName = inactiveSortingLayerName;
        animator.SetTrigger("die");
    }


    bool IsPlayerInRange() {
        Collider2D inRange = Physics2D.OverlapCircle(transform.position + new Vector3(0, 0.25f), range, playerLayer);
        if (inRange) {
            Vector2 dir = inRange.transform.position + new Vector3(0, 0.25f) -
                          (transform.position + new Vector3(0, 0.25f));

            return !Physics2D.Raycast((Vector2)transform.position + new Vector2(0, 0.25f), dir, dir.magnitude, groundLayer);
        }
        return false;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
