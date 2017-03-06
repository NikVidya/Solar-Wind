using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTriggerScript : MonoBehaviour {

	bool wasTriggered = false;
	public Scene sceneToTrigger;

	void OnTriggerEnter2D(Collider2D other){
		if (sceneToTrigger != null && !wasTriggered) {
			wasTriggered = true;
			sceneToTrigger.StartScene ();
		}
	}
}
