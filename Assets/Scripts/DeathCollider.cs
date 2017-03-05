using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathCollider : GenericCollider {
    public RespawnManager manager;

    public override void OnHit() {
        manager.PlayerDeath();
    }
}