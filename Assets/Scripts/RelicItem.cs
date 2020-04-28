using UnityEngine;

public class RelicItem : MonoBehaviour {
    [SerializeField] private Relic relic;

    void Start() {
        GetComponentInChildren<SpriteRenderer>().sprite = relic.icon;
    }

    void Update() {
        if (Vector2.Distance(Inventory.Instance.transform.position, transform.position) < 0.6f) {
            Inventory.Instance.GainRelic(relic.name);
            Destroy(gameObject);
        }
    }
}
