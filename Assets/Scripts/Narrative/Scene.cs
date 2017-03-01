﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent (typeof (NarrativeUtils))]
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

	public NarrativeUtils lib { get; private set; }

	private Camera sceneCam;

	private Camera returnCam;

	void Start(){
		sceneCam = GetComponentInChildren<Camera> (true);
		if (sceneCam == null) {
			Debug.LogError ("Error: Scene could not obtain it's camera");
			return;
		}

		lib = GetComponent<NarrativeUtils> ();
		if (lib == null) {
			Debug.LogError ("Error: Scene could not obtain it's utils");
			return;
		}
	}

	public void StartScene(){
		if (startingSequence != null) {
			// Take control of the camera if we are supposed to
			if (takeCameraControl) {
				Debug.Log ("Taking control of the camera");
				returnCam = Camera.main; // Store the camera the scene should return to
				Camera.main.gameObject.SetActive(false); // Disable the main camera
				sceneCam.gameObject.SetActive(true); // Enable our camera
			}
			// Play the starting sequence
			startingSequence.Play ();
		} else { // This scene has no sequences!
			Debug.LogWarning("Scene was started but had no sequences");
			return;
		}
	}

	public void EndScene(){
		callbacks.Invoke ();
		// Return control to the camera we took it from
		if (takeCameraControl) {
			Debug.Log ("Returning control of the camera");
			returnCam.gameObject.SetActive(true); // Disable the main camera
			sceneCam.gameObject.SetActive(false); // Enable our camera
		}
	}
}
