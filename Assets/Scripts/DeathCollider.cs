using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathCollider : MonoBehaviour {
    public RespawnManager manager;

    void OnTriggerEnter2D(Collider2D col) {
        manager.PlayerDeath();
    }
}