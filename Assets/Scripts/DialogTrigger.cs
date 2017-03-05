using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogTrigger : GenericCollider {
    public Scene scene;

    public override void OnHit() {
        Debug.Log("Hit, should start scene");
        scene.StartScene();
    }
}
