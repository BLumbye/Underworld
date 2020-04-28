using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour {
    [Header("General")]
    [SerializeField] private int health = 3;
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private List<Sprite> emissionMaps;

    [Header("Summoning")]
    [SerializeField] private GameObject flyingGolemPrefab;
    [SerializeField] private float flyingGolemCooldown = 4f;
    [SerializeField] private List<Transform> flyingGolemSpawnPoints;
    [SerializeField] private GameObject rollingGolemPrefab;
    [SerializeField] private float rollingGolemCooldown = 6f;
    [SerializeField] private List<Transform> rollingGolemSpawnPoints;
    [SerializeField] private float summonRadius = 20f;

    [Header("Hitting")]
    [SerializeField] private Vector2 hitKnockback;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float hitRange;
    [SerializeField] private Vector2 hitCenter;

    // References
    private Animator animator;
    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;
    private CameraController camera;
    private Player player;

    // State
    private bool hitting = false;
    private bool summoningFlying = false;
    private bool summoningRolling = false;


    private float hitTime = -1f;
    private float flyingTime = -1f;
    private float rollingTime = -1f;

    void Start() {
        // Set references
        animator = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        sr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_EmissionColor", emissionColor);
        sr.SetPropertyBlock(propertyBlock);

        camera = Camera.main.GetComponent<CameraController>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    void Update() {
        if (emissionColor != propertyBlock.GetColor("_EmissionColor")) {
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            sr.SetPropertyBlock(propertyBlock);
        }

        // Check if within hit range
        if (IsPlayerInRange() && !hitting && !summoningFlying && !summoningRolling && hitTime + hitCooldown <= Time.time) {
            hitting = true;
            animator.SetTrigger("hit");
        } else if (IsPlayerInSummonRange() && !hitting && !summoningFlying && !summoningRolling && flyingTime + flyingGolemCooldown <= Time.time) {
            summoningFlying = true;
            animator.SetTrigger("summon");
        } else if (IsPlayerInSummonRange() && !hitting && !summoningFlying && !summoningRolling && rollingTime + rollingGolemCooldown <= Time.time) {
            summoningRolling = true;
            animator.SetTrigger("summon");
        }
    }

    bool IsPlayerInRange() {
        return Physics2D.OverlapCircle((Vector2) transform.position + hitCenter, hitRange, playerLayer);
    }

    bool IsPlayerInSummonRange() {
        return Physics2D.OverlapCircle((Vector2)transform.position + hitCenter, summonRadius, playerLayer);
    }

    void HitFinished() {
        camera.trauma += 0.35f;
        if (IsPlayerInRange()) {
            player.Damage();
            player.Knockback(hitKnockback);
        }

        hitting = false;
        hitTime = Time.time;
    }

    void SummonFinished() {
        if (summoningFlying) {
            Instantiate(flyingGolemPrefab, flyingGolemSpawnPoints[Random.Range(0, flyingGolemSpawnPoints.Count)].position,
                Quaternion.identity);
            summoningFlying = false;
            flyingTime = Time.time;
        } else if (summoningRolling) {
            Instantiate(rollingGolemPrefab, rollingGolemSpawnPoints[Random.Range(0, rollingGolemSpawnPoints.Count)].position,
                Quaternion.identity);
            summoningRolling = false;
            rollingTime = Time.time;
        }
    }

    void Damage() {
        health--;
        propertyBlock.SetTexture("_Emission", emissionMaps[health - 1].texture);
        sr.SetPropertyBlock(propertyBlock);
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere((Vector3) hitCenter + transform.position, hitRange);
    }
}
