using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour {
    [SerializeField, Scene] private int scene;
    [SerializeField] private LayerMask playerLayer;

    private BoxCollider2D collider;

    void Start() {
        collider = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update() {
        if (Physics2D.OverlapBox(collider.offset + (Vector2)transform.position, collider.size, 0f, playerLayer)) {
            SceneManager.LoadScene(scene);
        }
    }
}
