using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public class PathingAgent : MonoBehaviour {

	public float speed = 2;
	public float acceleration = 0.5f;
	public float height = 0.5f;
	[Tooltip("Amount of time before the agent will teleport to target in seconds")]
	public float maxPathingTime = 3f; // In seconds

	public Transform target;

	private CapsuleCollider2D collider;
	private float curSpeed = 0;
	private float targetPlatform;
	private Vector2 targetPosition;
	private List<PathablePlatform.PlatformConnection> path = new List<PathablePlatform.PlatformConnection>();
	private bool pathingFinished = true;

	// Use this for initialization
	void Start () {
		collider = GetComponent<CapsuleCollider2D> ();
		if (collider == null) {
			Debug.LogError ("Pathing Agent needs a capsule collider 2d");
			return;
		}
		// Put the agent on the ground
		RaycastHit2D hit = Physics2D.CapsuleCast(collider.bounds.center, collider.size, collider.direction, transform.rotation.eulerAngles.z, Vector2.down);
		if (hit != null && hit.collider != null) {
			transform.position = hit.point + new Vector2(0, height);
		}
		//FindPath ();
	}

	/*IEnumerator pathFindingCoroutine;
	void FindPath(){
		pathingFinished = false;
		if (pathFindingCoroutine != null) {
			StopCoroutine (pathFindingCoroutine);
		}
		pathFindingCoroutine = FindPathCoroutine ();
		StartCoroutine (pathFindingCoroutine);
	}*/

	// Bredth first search, starting with this agent's platform
	/*IEnumerator FindPathCoroutine(){
		// Get the platform the agent is standing on
		PathablePlatform curPlatform = null;
		RaycastHit2D hit = Physics2D.CapsuleCast(collider.bounds.center, collider.size, collider.direction, transform.rotation.eulerAngles.z, Vector2.down);
		if (hit != null && hit.collider != null) {
			curPlatform = hit.collider.gameObject.GetComponent<PathablePlatform> ();
		}
		if (curPlatform == null) {
			Debug.LogWarning ("Pathing agent is on non pathable surface, can't navigate off");
			pathingFinished = true;
			return;
		}


	}*/

	// Update is called once per frame
	void Update () {
		
	}
}
