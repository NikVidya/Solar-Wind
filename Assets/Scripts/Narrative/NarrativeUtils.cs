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


	public delegate void UtilDoneCallback ();
	/**
	 * SpeakLine
	 * Causes a speech bubble containing the specified line to appear above the specified actor.
	 * Will also move other speech bubbles off the screen
	 */
	//private IEnumerator bubbleCoroutine;
	public void SpeakLine(GameObject actor, string line, int wordTime, UtilDoneCallback callback){
		//float speakTime = wordTime * line.Split (new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries).Length;

		// Meat of this one is done within the bubble script. 
		//Create a new speech bubble instance
		GameObject bubble = Instantiate (Resources.Load (Constants.Resources.Narrative.PREFAB_SPEECH_BUBBLE_PATH, typeof(GameObject)), sceneCanvas.gameObject.transform) as GameObject; // Parent the speech bubble to the scene canvas
		SpeechBubble bubbleScript = bubble.GetComponentInChildren<SpeechBubble> (true);
		if (bubbleScript == null) {
			Debug.LogError ("Created a speech bubble without a script. What?! Terminating stage and moving to next.");
			DestroyImmediate (bubble);
			callback ();
			return;
		}
		// Initialize the speech bubble
		bubbleScript.Initialize(actor, line, wordTime, callback);

		// Delay the return of the coroutine for sequence timing
		/*bubbleCoroutine = HandleSceneText (line, speakTime, callback);
		StartCoroutine (bubbleCoroutine);*/
	}
	/*private IEnumerator HandleSceneText(string line, float waitTime, UtilDoneCallback callback){
		yield return new WaitForSeconds (waitTime / 1000);
		//Debug.LogFormat ("Actor spoke line: {0}, for {1}ms", line, waitTime);
		// Done with this direction
		callback ();
	}*/

	public void PoseDialogOptions(GameObject actor, Sequence.SequenceChoice[] decisions, UtilDoneCallback callback){
		GameObject bubble = Instantiate (Resources.Load (Constants.Resources.Narrative.PREFAB_DECISION_BUBBLE_PATH, typeof(GameObject)), sceneCanvas.gameObject.transform) as GameObject;
		DecisionBubble bubbleScript = bubble.GetComponentInChildren<DecisionBubble> (true);
		if (bubbleScript == null) {
			Debug.LogError ("Created a speech bubble without a script. What?! Terminating stage and moving to next.");
			DestroyImmediate (bubble);
			callback ();
			return;
		}

		bubbleScript.Initialize (actor, decisions, callback);
	}
}
