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
}
