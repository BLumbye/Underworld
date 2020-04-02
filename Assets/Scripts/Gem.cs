using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

public class Gem : MonoBehaviour {
    [Serializable]
    public class GemAppearance {
        public Sprite sprite;
        public Gradient trailGradient;
    }

    // Right now the gem goes to the player as soon as it is spawned, a magnet with a range could be implemented later

    // A vector with a random angled (limited) and magnitude is given to the gem on spawn
    // This is to get the gem to fly using a curve, instead of linearly towards the player.
    // Alternatively a bezier curve could be used, but that is hard to deal with if the player moves.

    [Header("Follow settings")]
    [SerializeField, Tooltip("The starting maximum angle the velocity can have. This is away from the player."), Range(0f, 360f)]
    private float spawnAngleRange = 120;
    [SerializeField, Tooltip("The range that the magnitude of the starting velocity can have."), MinMaxSlider(0.0f, 20.0f)]
    private Vector2 spawnVelocityRange = new Vector2(3,6);
    [SerializeField]
    private float acceleration = 2;
    [SerializeField, Range(0f, 100f)]
    private float angleAdjustment = 5f;
    [SerializeField]
    private float maxSpeed = 20;

    [Header("Appearance")]
    [SerializeField, ReorderableList]
    private List<GemAppearance> variations = new List<GemAppearance>();

    private Vector2 velocity;
    private Transform target;

    void Start() {
        target = GameObject.FindGameObjectWithTag("Player").transform;

        // Choose random appearance
        GemAppearance appearance = variations[Random.Range(0, variations.Count)];
        GetComponent<SpriteRenderer>().sprite = appearance.sprite;
        GetComponent<TrailRenderer>().colorGradient = appearance.trailGradient;

        // Set the starting velocity
        float targetToGemAngle = Vector2.Angle(target.position, transform.position);
        float angle = Random.Range(-spawnAngleRange, spawnAngleRange) + targetToGemAngle + 180;
        float magnitude = Random.Range(spawnVelocityRange.x, spawnVelocityRange.y);
        velocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * magnitude;
    }

    void Update() {
        transform.position += (Vector3) velocity * Time.deltaTime;

        // Calculate the velocity
        velocity += (Vector2) (target.position - transform.position).normalized * acceleration * Time.deltaTime;

        // Lerp the velocity towards the player
        velocity = Vector2.Lerp(velocity, (target.position - transform.position).normalized * velocity.magnitude, angleAdjustment * Time.deltaTime);

        // Limit velocity (sqr is used as it is faster than normal mag, since that requires sqr root)
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed) {
            velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        }

        // Rotate the gem according to the velocity
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Vector2.SignedAngle(velocity, Vector2.right) - 90);

        // Collect gem when it is within collider
        if (Vector2.Distance(target.position, transform.position) < 0.5f) {
            Destroy(gameObject);
            // Spawn a particle effect?
        }
    }
}
