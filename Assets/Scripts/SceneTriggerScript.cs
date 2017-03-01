using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTriggerScript : MonoBehaviour {

	/***
	 *      ____  _____ ____  _   _  ____    ___  _   _ _  __   __
	 *     |  _ \| ____| __ )| | | |/ ___|  / _ \| \ | | | \ \ / /
	 *     | | | |  _| |  _ \| | | | |  _  | | | |  \| | |  \ V / 
	 *     | |_| | |___| |_) | |_| | |_| | | |_| | |\  | |___| |  
	 *     |____/|_____|____/ \___/ \____|  \___/|_| \_|_____|_|  
	 *                                                            
	 * 		I'm only including this script as a way of demonstrating
	 * 		how to hook world events into the scene system
	 * 
	 * 		Please don't use this in the actual game. I'll be 
	 * 		deleting it once we have some levels done. Oh, and you'll
	 * 		make a kitten cry or something else horrible will happen
	 * 		to you.
	 */

	bool wasTriggered = false;
	public Scene sceneToTrigger;

	void OnTriggerEnter2D(Collider2D other){
		if (sceneToTrigger != null && !wasTriggered) {
			wasTriggered = true;
			sceneToTrigger.StartScene ();
		}
	}
}
