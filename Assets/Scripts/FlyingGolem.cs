using UnityEngine;

public class FlyingGolem : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float range = 6f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor;
    [SerializeField] private float speed = 16f;
    [SerializeField] private float accelerationTime = 0.5f;
    [SerializeField] private float playerKnockbackForce;
    [SerializeField] private GameObject deathParticle;
    [SerializeField] private float rayLength = 0.25f;
    [SerializeField] private SpriteRenderer bodySr;

    private Animator animator;
    private MaterialPropertyBlock propertyBlock;
    private Controller2D controller;
    private Player player;

    private Vector2 velocity;
    private Vector2 velocitySmoothing;
    private Vector2 direction;

    private bool dashing = false;
    private bool preparingDash = false;

    void Start() {
        controller = GetComponent<Controller2D>();
        animator = GetComponent<Animator>();
        propertyBlock = new MaterialPropertyBlock();
        bodySr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_EmissionColor", emissionColor);
        bodySr.SetPropertyBlock(propertyBlock);

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    void Update() {
        if (emissionColor != propertyBlock.GetColor("_EmissionColor")) {
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            bodySr.SetPropertyBlock(propertyBlock);
        }

        bool playerInRange = IsPlayerInRange();
        if (playerInRange && !dashing && !preparingDash) {
            animator.SetTrigger("prepareDash");
            preparingDash = true;
        } else if (playerInRange && preparingDash) {
            // Set direction
            direction = (player.transform.position - new Vector3(0, 0.2f)) - transform.position;
            transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.down, direction));
        } else if (dashing) {
            Vector2 targetVelocity = direction.normalized * speed;
            velocity = Vector2.SmoothDamp(velocity, targetVelocity, ref velocitySmoothing, accelerationTime);

            // Check for ground collision
            RaycastHit2D hitGround = Physics2D.Raycast(transform.position, velocity, velocity.magnitude * Time.deltaTime + rayLength, groundLayer);
            if (hitGround) {
                Debug.Log("Ground!");
                Kill();
            }

            // Check for ground collision
            RaycastHit2D hitPlayer = Physics2D.Raycast(transform.position, velocity, velocity.magnitude * Time.deltaTime + rayLength, playerLayer);
            if (hitPlayer) {
                player.Damage();
                player.Knockback(direction.normalized * playerKnockbackForce);
                Kill();
            }

            transform.position += (Vector3) velocity * Time.deltaTime;
        }
    }

    public void PreparingDashFinished() {
        preparingDash = false;
        dashing = true;
    }

    public void Kill() {
        Instantiate(deathParticle, transform.position, Quaternion.identity);
        Destroy(gameObject);
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
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position - new Vector3(0, rayLength));
    }
}
