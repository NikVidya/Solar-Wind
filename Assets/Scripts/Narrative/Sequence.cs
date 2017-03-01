using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

public class Sequence : MonoBehaviour {

	[Header("Sequence Defaults")]
	[Tooltip("Default number of milliseconds to give to each word in the dialog")]
	public int defaultWordTime = 400;

	[Header("Sequence Data")]
	[Tooltip("The stage direction for this sequence")]
	public TextAsset sequenceFile; // The sequence to play

	// A choice presented to the user at the end of the sequence
	[System.Serializable]
	public class SequenceChoice
	{
		public string choiceText;
		public Sequence nextSequence;
		public UnityEvent eventsForChoice;
	}

	// List of choices to present to the user at the end of the sequence
	public SequenceChoice[] dialogOptions; // The choices the player can make
	[Tooltip("If there are no choices, this 'choice' will be made. If the next sequence is null, the scene will end.")]
	public SequenceChoice defaultAction; // The default actions that will happen if there are no choices

	// Storage for the parsed sequence file
	private class StageDirection
	{
		public string func;
		public List<string> data = new List<string> ();
	}
	private List<StageDirection> directions = new List<StageDirection> (); // List of stage directions in the file
	private int curDirectionIndex = 0;
	private bool sequenceOver = false;

	private Scene parentScene;

	void Awake(){
		// Begin parsing the sequence file
		// First, throw out comments
		Regex rgx = new Regex (@"\/\*(\*(?!\/)|[^*])*\*\/"); // Find block comments
		string sceneText = rgx.Replace(sequenceFile.text, "");
		rgx = new Regex (@"\/\/.*\n"); // Find single line comments
		sceneText = rgx.Replace(sceneText, "");

		// Now split the file into lines
		string[] sequence_lines = sceneText.Split (new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
		// Regular expression to extract the function name
		Regex funcRgx = new Regex (@"([a-zA-Z0-9]*)\(.*\);");
		// Regular expression to extract the function parameters
		Regex paramRgx = new Regex (@"(?<param>[0-9]+)|""(?<param>.*)""");
		// For each line, extract the function name and parameters into a StageDirection class and store it
		for (int i = 0; i < sequence_lines.Length; i++) {
			StageDirection dir = new StageDirection ();
			Match func = funcRgx.Match(sequence_lines[i]);
			dir.func = func.Groups [1].Value;
			MatchCollection data = paramRgx.Matches (sequence_lines[i]);
			for(int j=0; j < data.Count; j++ ){
				dir.data.Add( data [j].Groups ["param"].Value );
			}
			directions.Add (dir);
		}
		// Get a reference to the scene
		parentScene = GetComponentInParent<Scene>();
		if (parentScene == null) {
			Debug.LogError ("Sequence could not obtain parent scene");
		}
	}

	public void Play(){
		PlayDirection (); // Play the first direction
	}

	protected void PlayDirection(){
		StageDirection dir = directions [curDirectionIndex];
		// Figure out what to do as the stage direction
		switch (dir.func) {
		case "speakLine":
			int actorIndex = int.Parse (dir.data [0]);
			if (actorIndex > parentScene.actors.Length) {
				Debug.LogWarningFormat ("Attempted to give actor index {0} a direction, but that actor index is not in the scene!", actorIndex);
				return;
			}
			GameObject actor = parentScene.actors [actorIndex];
			int wordTime;
			if ( dir.data.Count < 3 || !int.TryParse (dir.data [2], out wordTime)) {
				wordTime = defaultWordTime; // Default to 400 ms
			}
			parentScene.lib.SpeakLine (actor, dir.data [1], wordTime, this);
			return;

		default:
			Debug.LogWarning (string.Format ("Sequence attempted to call unknown direction: {0}", dir.func));
			Next ();
			return;
		}
	}

	public void MakeDecision(SequenceChoice choice){
		if (choice == null) {
			Debug.LogWarning ("Sequence ended with null decision, Ending scene");
			sequenceOver = true; // This sequence should no longer be playing
			parentScene.EndScene (); // No choice made, can't continue so end the scene
		}

		choice.eventsForChoice.Invoke (); // Invoke any handlers for this choice

		if (choice.nextSequence != null) {
			sequenceOver = true; // This sequence should no longer be playing
			choice.nextSequence.Play (); // Start up the next sequence
		} else {
			defaultAction.eventsForChoice.Invoke (); // Invoke any default events
			if (defaultAction.nextSequence != null) {
				sequenceOver = true;
				defaultAction.nextSequence.Play ();
			} else {
				sequenceOver = true;
				parentScene.EndScene ();
			}
		}
	}

	public void Next(){
		if (sequenceOver) {
			Debug.LogWarning ("Attempted to move to the next direction on a terminated sequence");
			return; // Don't continue to the next direction if this sequence was marked as complete
		}
		if (curDirectionIndex < directions.Count - 1) { // There is another direction
			curDirectionIndex++;
			PlayDirection ();
		} else {
			if (dialogOptions.Length > 0) {
				parentScene.lib.PoseDialogOptions (parentScene.actors [0], dialogOptions, this);
			} else {
				defaultAction.eventsForChoice.Invoke (); // Invoke any default events
				if (defaultAction.nextSequence != null) {
					sequenceOver = true;
					defaultAction.nextSequence.Play ();
				} else {
					sequenceOver = true;
					parentScene.EndScene ();
				}
			}
		}
	}
}
