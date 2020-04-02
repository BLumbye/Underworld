using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {
    // The inventory stores the owned relics and currency

    public static Inventory Instance;

    public int currency = 0;
    public Relics relics;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            enabled = false;
        }
    }

    [Flags]
    public enum Relics {
        None = 0,
        DoubleJump = 1 << 0,
        GrapplingHook = 1 << 1,
        WallJump = 1 << 2,
    }
}
