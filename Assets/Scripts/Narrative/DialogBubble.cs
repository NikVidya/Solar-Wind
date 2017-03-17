using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class DialogBubble : MonoBehaviour {

	// Global parameters
	public float heightOffset = 1.0f;
	public GameObject actorIndicator;
	[HideInInspector]
	public Camera sceneCamera;

	// Obtained during initialization
	private float lifespan;
	private GameObject actor;
	private bool hasFinished = false;
	private bool hasBeenShown = false;

	// Obtained on Awake
	private CanvasGroup speechGroup;
	private Animator bubbleAnimator;
	private AudioSource audioSource;

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

		audioSource = GetComponentInParent<AudioSource> ();
		if (audioSource == null) {
			Debug.LogError ("Error: Speech bubble was not a descendant of something with an audio source. Can't play sound effects");
		}

		OnAwake ();
	}
	protected abstract void OnAwake ();

	public virtual void Initialize(GameObject actor, float lifespan){
		this.actor = actor;
		this.lifespan = lifespan;

		transform.parent.position = new Vector3 (actor.transform.position.x, actor.transform.position.y + heightOffset, actor.transform.position.z);
		bubbleAnimator.SetTrigger ("bubble_in");
	}

	public void HandleBubbleInFinished(){
		inTime = Time.time;
		hasBeenShown = true;
	}

	public void HandleBubbleOutFinished(){
		OnBubbleFinished ();
		Destroy (transform.parent.gameObject); // kill the parent, and therefore kill the self
	}
	protected abstract void OnBubbleFinished();


	public void DismissBubble(){
		hasFinished = true; // Prevent a double dismiss
		bubbleAnimator.SetTrigger ("bubble_out");
	}

	public void PlaySound(AudioClip clip){
		if (audioSource != null) {
			audioSource.PlayOneShot (clip);
		}
	}

	// Update is called once per frame
	void Update () {
		OnUpdate ();
	}
	void LateUpdate(){
		UpdatePosition ();
	}

	protected virtual void OnUpdate(){
		if ( hasBeenShown && lifespan > 0 && Time.time - inTime > lifespan && !hasFinished) { // Only consider bubbling out from lifespan if there was a valid lifespan
			hasFinished = true;
			bubbleAnimator.SetTrigger ("bubble_out");
		}
	}

	void UpdatePosition(){
		if (actor == null) {
			return;
		}

		transform.position = actor.transform.position + new Vector3 (0, heightOffset + 0.05f, 0);
		actorIndicator.transform.position = actor.transform.position + new Vector3 (0, heightOffset, 0);
	}
}
