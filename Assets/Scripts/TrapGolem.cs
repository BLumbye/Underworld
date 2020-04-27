using System.Security.Cryptography;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TrapGolem : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float range;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileEmissionRadius = 0.5f;
    [SerializeField] private Vector2 projectileEmissionCenter = new Vector2(0, 0.25f);
    [SerializeField] private int projectileCount = 5;
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor;
    [SerializeField] private GameObject deathParticle;

    private bool growing = false;
    private bool grown = false;

    private Animator animator;
    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;

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
        }

        bool playerInRange = IsPlayerInRange();
        if (playerInRange && !grown && !growing) {
            growing = true;
            animator.SetTrigger("grow");
        } else if (playerInRange && grown) {
            Explode();
        }
    }

    void Explode() {
        float angleDifference = 180f / (projectileCount - 1);
        for (int i = 0; i < projectileCount; i++) {
            float angle = i * angleDifference;
            Vector3 position = transform.position + (Vector3) projectileEmissionCenter + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * projectileEmissionRadius;
            Instantiate(projectilePrefab, position, Quaternion.Euler(0, 0, angle));
        }

        Instantiate(deathParticle, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void GrowingFinished() {
        grown = true;
    }

    bool IsPlayerInRange() {
        return Physics2D.OverlapCircle(transform.position, range, playerLayer) != null;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + (Vector3)projectileEmissionCenter, projectileEmissionRadius);
    }
}