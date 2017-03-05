using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogTrigger : GenericCollider {
    public Scene scene;
    void Start() {
        scene.StartScene();
    }
	void OnHit() {
        base.OnHit();
        scene.StartScene();
    }
}
