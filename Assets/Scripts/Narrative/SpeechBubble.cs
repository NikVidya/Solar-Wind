using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubble : DialogBubble {

	private string line; // The line of dialog to display
	private Text line_text; // The UI text to use for displaying the line

	protected override void OnAwake(){
		line_text = GetComponentInChildren<Text> ();
		if (line_text == null) {
			Debug.LogError ("Error: Speech bubble was unable to aquire its internal text");
		}
	}

	public void Initialize(GameObject actor, string line, int wordTime, NarrativeUtils.UtilDoneCallback callback){
		// Determine how long the bubble should last for
		float maxAge = wordTime * line.Split (new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries).Length / 1000.0f;
		// Set up the line of dialog
		this.line = line;
		line_text.text = this.line;

		base.Initialize (actor, maxAge, callback);
	}

}
