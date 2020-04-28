﻿using System.Security.Cryptography;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TrapGolem : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float range;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileEmissionRadius = 0.5f;
    [SerializeField] private Vector2 projectileEmissionCenter = new Vector2(0, 0.25f);
    [SerializeField] private float projectileSpeed = 12f;
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
        sr.SetPropertyBlock(propertyBlock);
    }

    void Update() {
        if (emissionColor != propertyBlock.GetColor("_EmissionColor")) {
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            sr.SetPropertyBlock(propertyBlock);
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
            TrapGolemProjectile projectile = Instantiate(projectilePrefab, position, Quaternion.Euler(0, 0, angle)).GetComponent<TrapGolemProjectile>();
            projectile.speed = projectileSpeed;
        }

        Instantiate(deathParticle, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void GrowingFinished() {
        grown = true;
    }

    bool IsPlayerInRange() {
        Collider2D inRange = Physics2D.OverlapCircle((Vector2) transform.position + projectileEmissionCenter, range, playerLayer);
        if (inRange) {
            Vector2 dir = inRange.transform.position + new Vector3(0, 0.25f) -
                          (transform.position + (Vector3) projectileEmissionCenter);

            return !Physics2D.Raycast((Vector2)transform.position + projectileEmissionCenter, dir, dir.magnitude, groundLayer);
        }
        return false;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + (Vector3) projectileEmissionCenter, range);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + (Vector3)projectileEmissionCenter, projectileEmissionRadius);
    }
}