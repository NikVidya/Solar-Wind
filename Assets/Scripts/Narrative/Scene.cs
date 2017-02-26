using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Scene : MonoBehaviour {

	[Header("Scene Properties")]
	[Space(5)]
	public bool takeCameraControl = true;
	public bool takePlayerControl = true;
	public bool isReplayable = false;
	public int speechTime = 400;
	[Space(10)]

	[Header("Scene Data")]
	[Space(5)]
	public Canvas sceneCanvas;
	[Space(5)]
	[Tooltip("List of actors in the scene - Does not include player by default")]
	public GameObject[] actors;
	[Space(5)]
	[Tooltip("List of spikes. Objects which define where actors should move to")]
	public GameObject[] spikes;

	public Sequence startingSequence; // The sequence script to start the scene with

	[Space(10)]

	[Header("Scene End Callbacks")]
	[Tooltip("Add functions here to have them run at the end of the scene")]
	public UnityEvent callbacks;

	void Start(){
		startingSequence.Play ();
	}

	public void EndScene(){
		callbacks.Invoke ();
	}
}
