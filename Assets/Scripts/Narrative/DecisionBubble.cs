using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DecisionBubble : DialogBubble {

	public GameObject decisionContainer;
	public GameObject decisionPrefab;
	public Text nameText; // The UI element to indicate who is talking
	private Sequence.SequenceChoice[] decisions;

	// Delegate function for handling callback
	public delegate void BubbleEndCallback(Sequence.SequenceChoice choice);
	private BubbleEndCallback callback;

	private bool hasMadeChoice = false;
	private Sequence.SequenceChoice finalChoice;

	protected override void OnAwake()
	{
		
	}

	public void Initialize(GameObject actor, Sequence.SequenceChoice[] decisions, BubbleEndCallback callback){
		this.decisions = decisions;
		this.callback = callback;

		nameText.text = actor.gameObject.name;
		RefreshDecisions ();

		base.Initialize (actor, -1);
	}

	private void RefreshDecisions(){
		Debug.LogFormat ("Showing decisions from list with length: {0}", decisions.Length);
		for (int i = 0; i < decisions.Length; i++) {
			GameObject decision = Instantiate (decisionPrefab, decisionContainer.transform);
			decision.transform.localScale = new Vector3 (1, 1, 1);
			Text text = decision.GetComponentInChildren<Text> ();
			text.text = decisions [i].choiceText;
			Button button = decision.GetComponent<Button> ();
			int captured_i = i;
			button.onClick.AddListener( () => {
				MakeChoice(captured_i);
			} );
		}
	}
	public void MakeChoice(int choiceIndex){
		if (!hasMadeChoice) { // Only alow a choice to be made while we haven't already made one.
			hasMadeChoice = true;
			DismissBubble ();
			finalChoice = decisions [choiceIndex];
		}
	}
		
	protected override void OnBubbleFinished(){
		callback (finalChoice); // Call the callback with our final choice
	}
}
