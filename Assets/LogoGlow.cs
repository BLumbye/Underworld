using UnityEngine;

public class LogoGlow : MonoBehaviour {
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor;

    // Start is called before the first frame update
    void Start() {
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_EmissionColor", emissionColor);
        sr.SetPropertyBlock(propertyBlock);
    }
}
