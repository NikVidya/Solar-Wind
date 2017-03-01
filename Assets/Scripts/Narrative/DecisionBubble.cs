using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DecisionBubble : DialogBubble {

	public GameObject decisionPrefab;
	private Sequence.SequenceChoice[] decisions;

	protected override void OnAwake()
	{
		
	}

	public void Initialize(GameObject actor, Sequence.SequenceChoice[] decisions, NarrativeUtils.UtilDoneCallback callback){
		this.decisions = decisions;

		RefreshDecisions ();

		base.Initialize (actor, -1, callback);
	}

	private void RefreshDecisions(){
		for (int i = 0; i < decisions.Length; i++) {
			GameObject decision = Instantiate (decisionPrefab, transform);
			decision.transform.localScale = new Vector3 (1, 1, 1);
			Text text = decision.GetComponentInChildren<Text> ();
			text.text = decisions [i].choiceText;
			Button button = decision.GetComponent<Button> ();
			int captured_i = i;
			button.onClick.AddListener( () => {
				Debug.LogFormat("You clicked on option {0}", captured_i);
			} );
		}
	}

}
