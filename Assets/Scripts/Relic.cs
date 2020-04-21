using UnityEngine;

[CreateAssetMenu(fileName = "New Relic", menuName = "Scriptable Objects/Relic")]
public class Relic : ScriptableObject {
    // Constants
    public new string name;
    public string description;
    public Sprite icon;

    // Variables
    public bool owned;
}
