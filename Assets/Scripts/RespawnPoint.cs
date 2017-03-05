using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : GenericCollider {
    public RespawnManager manager;

    public override void OnHit() {
        manager.checkPoint = transform;
    }
}
