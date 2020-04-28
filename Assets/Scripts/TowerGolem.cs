using UnityEngine;

public class TowerGolem : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float range;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float shootCooldown = 1f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Vector2 projectileEmission = new Vector2(0, 0.25f);
    [SerializeField] private float projectileSpeed = 12f;
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor;
    [SerializeField] private GameObject deathParticle;

    private Animator animator;
    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;

    private bool shooting = false;
    private float shootTime = -1f;

    void Start() {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        sr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_EmissionColor", emissionColor);
        GetComponent<SpriteRenderer>().SetPropertyBlock(propertyBlock);
    }

    void Update() {
        if (emissionColor != propertyBlock.GetColor("_EmissionColor")) {
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            GetComponent<SpriteRenderer>().SetPropertyBlock(propertyBlock);
        }

        bool playerInRange = IsPlayerInRange();
        if (playerInRange && !shooting && shootTime + shootCooldown <= Time.time) {
            shooting = true;
            animator.SetTrigger("shoot");
        }
    }

    public void Shoot() {
        shooting = false;
        shootTime = Time.time;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 direction = (player.transform.position + new Vector3(0, 0.5f)) - (transform.position + (Vector3) projectileEmission);
        float angle = Vector2.SignedAngle(Vector2.right, direction);
        TrapGolemProjectile projectile =
            Instantiate(projectilePrefab, transform.position + (Vector3) projectileEmission,
                Quaternion.Euler(0, 0, angle)).GetComponent<TrapGolemProjectile>();
        projectile.speed = projectileSpeed;
    }

    bool IsPlayerInRange() {
        Collider2D inRange = Physics2D.OverlapCircle((Vector2) transform.position + projectileEmission, range, playerLayer);
        if (inRange) {
            return !Physics2D.Raycast((Vector2) transform.position + projectileEmission,
                (Vector2) (inRange.transform.position - transform.position) + projectileEmission, range, groundLayer);
        }
        return false;
    }

    public void Kill() {
        Instantiate(deathParticle, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + (Vector3) projectileEmission, range);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + (Vector3)projectileEmission, 0.2f);
    }
}
