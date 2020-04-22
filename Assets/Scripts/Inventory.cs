using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {
    // The inventory stores the owned relics and currency

    public static Inventory Instance;

    public int currency = 0;
    [SerializeField] private List<Relic> relics;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            enabled = false;
        }
    }

    public Relic GetRelic(string name) {
        return relics.Find(r => r.name == name);
    }

    public bool HasRelic(string name) {
        return GetRelic(name).owned;
    }

    public void GainAbility(string name) {
        GetRelic(name).owned = true;
    }
}
