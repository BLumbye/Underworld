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

    private bool growing = false;
    private bool grown = false;

    private Animator animator;

    void Start() {
        animator = GetComponent<Animator>();
    }

    void Update() {
        bool playerInRange = IsPlayerInRange();
        if (playerInRange && !grown && !growing) {
            growing = true;
            animator.SetTrigger("grow");
        } else if (playerInRange && grown) {
            Explode();
        }
    }

    void Explode() {
        float angleDifference = 180f / projectileCount;
        for (int i = 0; i < projectileCount; i++) {
            float angle = -i * angleDifference;
            Vector3 position = transform.position + (Vector3) projectileEmissionCenter + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * projectileEmissionRadius;
            Instantiate(projectilePrefab, position, Quaternion.Euler(0, 0, angle));
        }

        Destroy(gameObject);
    }

    void GrowingFinished() {
        animator.SetBool("grown", true);
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