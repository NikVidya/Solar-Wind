using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubble : DialogBubble {

	public Text nameText; // The UI element to indicate who is talking
	public Text dialogText; // The UI text to use for displaying the line

	private string line; // The line of dialog to display

	// Delegate function for handling callback
	public delegate void BubbleEndCallback();
	private BubbleEndCallback callback;

	protected override void OnAwake(){
	}


	public void Initialize(GameObject actor, string line, int wordTime, BubbleEndCallback callback){
		// Determine how long the bubble should last for
		float maxAge = wordTime * line.Split (new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries).Length / 1000.0f;
		// Set up the line of dialog
		this.line = line;
		dialogText.text = this.line;

		nameText.text = actor.gameObject.name;

		this.callback = callback;

		base.Initialize (actor, maxAge);
	}

	protected override void OnBubbleFinished(){
		callback();
	}

}
