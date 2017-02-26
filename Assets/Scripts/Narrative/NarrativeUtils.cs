using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrativeUtils : MonoBehaviour {
	public delegate void UtilDoneCallback ();
	/**
	 * SpeakLine
	 * Causes a speech bubble containing the specified line to appear above the specified actor.
	 * Will also move other speech bubbles off the screen
	 */
	public void SpeakLine(GameObject actor, string line, UtilDoneCallback callback){
		Debug.Log (string.Format ("An actor spoke: {0}", line));
		callback ();
	}
}
