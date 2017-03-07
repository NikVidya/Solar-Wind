using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrativeUtils : MonoBehaviour {
	private Canvas sceneCanvas;
	private Camera sceneCamera;

	void Start(){
		sceneCanvas = GetComponentInChildren<Canvas> (true);
		if (sceneCanvas == null) {
			Debug.LogError ("Error: Narrative Utilities for scene could not aquire its canvas");
		}

		sceneCamera = GetComponentInChildren<Camera> (true);
		if (sceneCamera == null) {
			Debug.LogError ("Error: Narrative Utilities for a scene could not aquire its camera");
		}
	}


	/**
	 * SpeakLine
	 * Causes a speech bubble containing the specified line to appear above the specified actor.
	 * Will also move other speech bubbles off the screen
	 */
	public void SpeakLine(GameObject actor, string line, int wordTime, Sequence sequence){
		//float speakTime = wordTime * line.Split (new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries).Length;

		// Meat of this one is done within the bubble script. 
		//Create a new speech bubble instance
		GameObject bubble = Instantiate (Resources.Load (Constants.Resources.Narrative.PREFAB_SPEECH_BUBBLE_PATH, typeof(GameObject)), sceneCanvas.gameObject.transform) as GameObject; // Parent the speech bubble to the scene canvas
		SpeechBubble bubbleScript = bubble.GetComponentInChildren<SpeechBubble> (true);
		if (bubbleScript == null) {
			Debug.LogError ("Created a speech bubble without a script. What?! Terminating direction and moving to next.");
			DestroyImmediate (bubble);
			sequence.Next ();
			return;
		}
		// Initialize the speech bubble
		bubbleScript.Initialize(actor, line, wordTime, ()=>{
			sequence.Next();
		});
	}

	public void PoseDialogOptions(GameObject actor, Sequence.SequenceChoice[] decisions, Sequence sequence){
		GameObject bubble = Instantiate (Resources.Load (Constants.Resources.Narrative.PREFAB_DECISION_BUBBLE_PATH, typeof(GameObject)), sceneCanvas.gameObject.transform) as GameObject;
		DecisionBubble bubbleScript = bubble.GetComponentInChildren<DecisionBubble> (true);
		if (bubbleScript == null) {
			Debug.LogError ("Created a speech bubble without a script. What?! Terminating direction and moving to next.");
			DestroyImmediate (bubble);
			sequence.Next ();
			return;
		}

		bubbleScript.Initialize (actor, decisions, (Sequence.SequenceChoice choice) => {
			sequence.MakeDecision(choice);
		});
	}

	IEnumerator moveCoroutine;
	public void MoveTo(GameObject actor, Vector3 target, float time, Sequence sequence){
		if (time > 0) {
			if (moveCoroutine != null) {
				StopCoroutine (moveCoroutine);
			}
			moveCoroutine = MoveToCoroutine (actor, target, time/1000, sequence);
			StartCoroutine (moveCoroutine);
		} else {
			actor.transform.position = target;
			sequence.Next ();
		}
	}
	IEnumerator MoveToCoroutine(GameObject actor, Vector3 target, float time, Sequence sequence){
		Vector3 start = actor.transform.position;
		float startTime = Time.time;
		Animator animator = actor.GetComponentInChildren<Animator> ();

		if (animator != null) {
			animator.SetFloat ("speed", 1);
			animator.SetInteger ("direction", (int)Mathf.Sign (target.x - actor.transform.position.x));
		}
		while (Time.time - startTime < time) {
			actor.transform.position = Vector3.Lerp (start, target, (Time.time - startTime) / time);
			yield return null;
		}
		if (animator != null) {
			animator.SetFloat ("speed", 0);
		}
		sequence.Next ();
	}

	IEnumerator delayCoroutine;
	public void Delay(float time, Sequence sequence){
		if (time <= 0) {
			sequence.Next ();
			return;
		}
		if (delayCoroutine != null) {
			StopCoroutine (delayCoroutine);
		}
		delayCoroutine = DelayCoroutine (time, sequence);
		StartCoroutine (delayCoroutine);
	}
	IEnumerator DelayCoroutine(float time, Sequence sequence){
		yield return new WaitForSeconds (time / 1000);
		sequence.Next ();
	}
}
