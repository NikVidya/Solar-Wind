using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour {

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

	public float doorLiftHeight = 2;
	public float moveTime = 1;
	private Vector3 restPos;
	private Vector3 targetPos;
	IEnumerator coroutine;

	void Start(){
		restPos = transform.position;
		targetPos = restPos + new Vector3 (0, doorLiftHeight, 0);
	}

	IEnumerator MoveDoor(){
		float start = Time.time;
		while (Vector3.Distance (transform.position, targetPos) > 0.1) {
			transform.position = Vector3.Lerp (restPos, targetPos, (Time.time - start) / moveTime);
			yield return null;
		}
	}
	public void OpenDoor(){
		if (coroutine != null) {
			StopCoroutine (coroutine);
		}
		coroutine = MoveDoor ();
		StartCoroutine (coroutine);
	}
}
