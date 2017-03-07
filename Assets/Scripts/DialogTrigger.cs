using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogTrigger : GenericCollider {
	public Scene scene;
	private bool scenePlayed = false;

	public override void OnHit() {
		if ( !scenePlayed ) {
			scenePlayed = true;
			scene.StartScene();
		}
	}
}
