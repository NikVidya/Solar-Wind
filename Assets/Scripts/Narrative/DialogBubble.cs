using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class DialogBubble : MonoBehaviour {

	// Global parameters
	public float updateSpeed = 10.0f;
	public float actorHeightOffset = 1.0f;

	// Obtained during initialization
	private float lifespan;
	private GameObject actor;
	private NarrativeUtils.UtilDoneCallback callback;

	// Obtained on Awake
	private CanvasGroup speechGroup;
	private Animator bubbleAnimator;

	// -----
	private float inTime; // When the bubble was fully on screen

	// Use this for initialization
	void Awake () {
		speechGroup = GetComponent<CanvasGroup> ();
		if (speechGroup == null) {
			Debug.LogError ("Error: Speech bubble was unable to aquire its canvas group");
		}

		bubbleAnimator = GetComponent<Animator> ();
		if (bubbleAnimator == null) {
			Debug.LogError ("Error: Speech bubble was unabel to aquire its animator");
		}

		OnAwake ();
	}
	protected abstract void OnAwake ();


	public virtual void Initialize(GameObject actor, float lifespan, NarrativeUtils.UtilDoneCallback callback){
		this.actor = actor;
		this.lifespan = lifespan;
		this.callback = callback;

		transform.parent.position = new Vector3 (actor.transform.position.x, actor.transform.position.y + actorHeightOffset, actor.transform.position.z);
		bubbleAnimator.SetTrigger ("bubble_in");
	}

	public void HandleBubbleInFinished(){
		inTime = Time.time;
	}

	public void HandleBubbleOutFinished(){
		Destroy (transform.parent.gameObject); // kill the parent, and therefore kill the self
		callback();
	}

	public void DismissBubble(){
		bubbleAnimator.SetTrigger ("bubble_out");
	}

	// Update is called once per frame
	void Update () {
		UpdatePosition ();
		OnUpdate ();
	}
	protected virtual void OnUpdate(){
		if ( lifespan > 0 && Time.time - inTime < lifespan) { // Only consider bubbling out from lifespan if there was a valid lifespan
			bubbleAnimator.SetTrigger ("bubble_out");
		}
	}

	void UpdatePosition(){
		if (Vector3.Distance (transform.parent.position, actor.transform.position) > 0.1f) {
			Vector3 targetPos = new Vector3 (actor.transform.position.x, actor.transform.position.y + actorHeightOffset, actor.transform.position.z);
			transform.parent.position = Vector3.Lerp (transform.parent.position, targetPos, Time.deltaTime * updateSpeed);
		}
	}
}
