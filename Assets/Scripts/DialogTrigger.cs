using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogTrigger : GenericCollider {
    public Scene scene;
    private bool hitOnce = false;

    public override void OnHit() {
        if (hitOnce == false) {
            Debug.Log("Hit, should start scene");
            scene.StartScene();
            hitOnce = true;
        }
    }
}
