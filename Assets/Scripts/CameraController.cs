using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class CameraController : MonoBehaviour {
    [Header("Follow settings")]
    public GameObject target;

    [Header("Smoothing")]
    [Label("Enabled")]
    public bool smoothingEnabled = false;
    [Tooltip("The smoothing of the horizontal movement. Higher is slower movement towards target."), Range(0f, 1f), EnableIf("smoothingEnabled")]
    public float xSmoothing = 10;
    [Tooltip("The smoothing of the vertical movement. Higher is slower movement towards target."), Range(0f, 1f), EnableIf("smoothingEnabled")]
    public float ySmoothing = 10;

    [Header("Deadzone")] 
    [Tooltip("Deadzone provides a margin in the center of the screen where the camera won't move."), Label("Enabled")]
    public bool deadzoneEnabled = false;
    [Tooltip("The percentage of the width of the screen that is in the deadzone."), Range(0f, 1f), EnableIf("deadzoneEnabled")]
    public float deadzoneWidth = 0.1f;
    [Tooltip("The percentage of the height of the screen that is in the deadzone."), Range(0f, 1f), EnableIf("deadzoneEnabled")]
    public float deadzoneHeight = 0.1f;

    [Header("Screenshake")]
    public AnimationCurve traumaCurve;
    public float traumaReduction;
    public Vector2 maxTranslation;
    public float maxAngle;
    public float speedScale;


    private new Camera camera;
    private Vector2 velocity = Vector2.zero;
    private Vector2 position;

    #region Screenshake variables
    private Vector2 shakeOffset = Vector2.zero;
    public float trauma;
    #endregion

    void Start() {
        camera = GetComponent<Camera>();
        position = new Vector3(target.transform.position.x, target.transform.position.y, -10);
    }

    // Late update is used since the camera should move after movement
    void LateUpdate() {
        Vector2 newPos = Vector2.zero;

        if (deadzoneEnabled && smoothingEnabled)
            newPos = SmoothingAndDeadzone(position, target.transform.position);
        else if (smoothingEnabled)
            newPos = Smoothing(position, target.transform.position);
        else if (deadzoneEnabled)
            newPos = Deadzone(position, target.transform.position);
        else
            newPos = target.transform.position;

        position = new Vector3(newPos.x, newPos.y, transform.position.z);
        UpdateScreenshake();
        transform.position = (Vector3) (position + shakeOffset) + new Vector3(0, 0, -10);
    }

    Vector2 SmoothingAndDeadzone(Vector2 position, Vector2 targetPosition) {
        // Horizontal and vertical deadzone is handled separately
        Vector2 newPosition = position;
        Vector2 deadzoneSize = GetDeadzoneSize();
        Vector2 delta = targetPosition - position;

        if (Mathf.Abs(delta.x) > deadzoneSize.x) {
            float target = targetPosition.x - Mathf.Sign(delta.x) * deadzoneSize.x;
            newPosition.x = Mathf.SmoothDamp(position.x, target, ref velocity.x, xSmoothing);
        } else {
            velocity.x = 0;
        }

        if (Mathf.Abs(delta.y) > deadzoneSize.y) {
            float target = targetPosition.y - Mathf.Sign(delta.y) * deadzoneSize.y;
            newPosition.y = Mathf.SmoothDamp(position.y, target, ref velocity.y, ySmoothing);
        } else {
            velocity.y = 0;
        }

        return newPosition;
    }

    Vector2 Smoothing(Vector2 position, Vector2 targetPosition) {
        Vector2 newPosition;
        newPosition.x = Mathf.SmoothDamp(position.x, targetPosition.x, ref velocity.x, xSmoothing);
        newPosition.y = Mathf.SmoothDamp(position.y, targetPosition.y, ref velocity.y, ySmoothing);
        return newPosition;
    }

    Vector2 Deadzone(Vector2 position, Vector2 targetPosition) {
        // Horizontal and vertical deadzone is handled separately
        Vector2 newPosition = position;
        Vector2 deadzoneSize = GetDeadzoneSize();
        Vector2 delta = targetPosition - position;

        if (Mathf.Abs(delta.x) > deadzoneSize.x) {
            newPosition.x = targetPosition.x - Mathf.Sign(delta.x) * deadzoneSize.x;
        }

        if (Mathf.Abs(delta.y) > deadzoneSize.y) {
            newPosition.y = targetPosition.y - Mathf.Sign(delta.y) * deadzoneSize.y;
        }

        return newPosition;
    }

    void UpdateScreenshake() {
        float shake = traumaCurve.Evaluate(trauma);

        shakeOffset = new Vector2(
            maxTranslation.x * shake * (Mathf.PerlinNoise(1000, Time.time * speedScale) * 2 - 1),
            maxTranslation.y * shake * (Mathf.PerlinNoise(2000, Time.time * speedScale) * 2 - 1)
        );

        transform.localEulerAngles = new Vector3(0, 0, maxAngle * shake * (Mathf.PerlinNoise(3000, Time.time * speedScale) * 2 - 1));

        trauma = Mathf.Max(0, trauma - traumaReduction * Time.deltaTime);
    }

    Vector2 GetDeadzoneSize() {
        // TODO: Add another mode that calculates the the deadzone in world space using units
        Vector2 camSize = new Vector2(camera.orthographicSize, camera.orthographicSize * camera.aspect);
        return new Vector2(deadzoneWidth * camSize.x, deadzoneHeight * camSize.y);
    }
}
