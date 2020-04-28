using UnityEngine;

public class Sign : MonoBehaviour {
    [TextArea, SerializeField]
    private string text;

    [SerializeField] private float radius = 0.75f;

    // Update is called once per frame
    void Update() {
        if (Vector2.Distance(Inventory.Instance.transform.position, transform.position) < radius) {
            Inventory.Instance.signText = text;
            Inventory.Instance.signPosition = transform.position;
        }

    }
}
