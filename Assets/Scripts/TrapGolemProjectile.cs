using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrapGolemProjectile : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] public float speed;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private string inactiveSortingLayerName = "ProjectilesInactive";
    [SerializeField] private List<Sprite> variations = new List<Sprite>();

    private float groundHitTime = -1;
    private float groundHitDelay = 0.03f;
    private float spriteWidth;

    private SpriteRenderer sr;

    private bool groundHit {
        get => groundHitTime > 0;
    }

    private float angle {
        get => transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
    }

    void Start() {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = variations[Random.Range(0, variations.Count)];
        spriteWidth = sr.sprite.bounds.size.x;
    }

    // Update is called once per frame
    void Update() {
        Vector2 toMove = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * Time.deltaTime;

        // If ground is hit and delay is passed then deactivate projectile
        if (groundHit && groundHitTime + groundHitDelay <= Time.time) {
            this.enabled = false;
            return;
        } else if (groundHit) {
            transform.position += (Vector3)toMove;
            return;
        }

        // Check if hitting ground
        RaycastHit2D hitGround = Physics2D.Raycast(transform.position, toMove, toMove.magnitude + spriteWidth, groundLayer);
        if (hitGround) {
            groundHitTime = Time.time;
            sr.sortingLayerName = inactiveSortingLayerName;
            return;
        }

        // Check if hitting player
        RaycastHit2D hitPlayer = Physics2D.Raycast(transform.position, toMove, toMove.magnitude + spriteWidth, playerLayer);
        if (hitPlayer) {
            hitPlayer.collider.GetComponent<Player>().Damage();
            Destroy(gameObject);
        }

        transform.position += (Vector3) toMove;
    }
}
