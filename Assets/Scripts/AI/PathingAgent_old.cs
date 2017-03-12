using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CapsuleCollider2D))]
public class PathingAgent_old : MonoBehaviour {

	[Header("Agent Movement Properties")]
	[Tooltip("Max travel speed in units/second")]
	public float maxSpeed = 5;
	[Tooltip("Acceleration rate in units/second^2")]
	public float acceleration = 4f;
	[Tooltip("Maximum unit height of jump")]
	public float jumpHeight = 3;
	[Tooltip("Maximum time to spend chasing target")]
	public float timeBeforeTeleport = 3;
	[Tooltip("Distance range to stop at the target")]
	public float endMaxDistance = 2;

	[Header("Agent Pathfinding Properties")]
	[Tooltip("Length of time to spend pathfinding (in milliseconds) (keep small to avoid framedrops)")]
	public float maxPathingTime = 20f; // In milliseconds
	[Tooltip("How much the target has to move before updating the path")]
	public float targetTolerance = 0.2f;
	[Tooltip("Minimum time between path updates in seconds")]
	public float minPathAge = 0.5f;

	public Transform target;

	private CapsuleCollider2D myCollider;
	private PathablePlatform currentPlatform;
	private List<PathablePlatform.PlatformConnection> path;
	private List<PathablePlatform> platformsConsidered = new List<PathablePlatform>();
	private int layerMask = 1 << 8;
	private float height;
	IEnumerator pathFindingCoroutine;
	IEnumerator movementCoroutine;
	PathablePlatform.PlatformConnection nextConnection = null;

	float curSpeed;

	void Start () {
		myCollider = GetComponent<CapsuleCollider2D> ();
		if (myCollider == null) {
			Debug.LogError ("Pathing Agent needs a capsule collider 2d");
			return;
		}
		height = myCollider.bounds.extents.y;
		// Put the agent on the ground
		PutOnGround();
	}

	bool isActioning = false;
	bool onLastPlatform = false;
	// Always move to the closest point on the current platform that makes the jump point within range
	Vector3 jumpTarget;
	Vector3 lastTargetPathPosition = new Vector3 ();
	float lastPathTime = 0;
	float chaseStartedTime;
	bool pathHasChanged = false;
	PathablePlatform targetPlatform;
	void Update(){
		// Get the platform the target is on
		// Get the target platform to move to
		RaycastHit2D hit = Physics2D.Raycast(target.transform.position + new Vector3(0,1,0), Vector2.down, Mathf.Infinity, layerMask);
		if ( hit.collider != null) {
			targetPlatform = hit.collider.gameObject.GetComponent<PathablePlatform>();
		}
		if (targetPlatform == currentPlatform && currentPlatform != null) {
			onLastPlatform = true;
		}

		// Handle path update
		if (Vector3.Distance (lastTargetPathPosition, target.position) > targetTolerance && (Time.time - lastPathTime) > minPathAge && !isActioning && !onLastPlatform ) {
			lastPathTime = Time.time;
			lastTargetPathPosition = target.position;
			onLastPlatform = false;
			chaseStartedTime = Time.time;
			pathHasChanged = true;

			// Clear out the current path, it's invalid
			if (path != null) {
				path.Clear ();
			}
			nextConnection = null;

			// Try and find the collider under me
			hit = Physics2D.CapsuleCast(myCollider.bounds.center + new Vector3(0, 0.1f), myCollider.size, myCollider.direction, 0, Vector2.down, height*2.0f, layerMask);
			if (hit.collider != null) {
				currentPlatform = hit.collider.gameObject.GetComponent<PathablePlatform>();
			}

			// Stop trying to find a path to the last target
			if (pathFindingCoroutine != null) {
				StopCoroutine (pathFindingCoroutine);
			}
			// Start finding a new path
			pathFindingCoroutine = FindPath ();
			StartCoroutine (pathFindingCoroutine);
		}

		// Handle agent movement
		// Move whenever there is a path
		if ( !isActioning && nextConnection != null && currentPlatform != null || onLastPlatform) {
			isActioning = true;
			// We are done with whatever movement was being done, so stop the coroutine and start the next one
			if (movementCoroutine != null) {
				StopCoroutine (movementCoroutine);
			}

			// Determine if we are about to jump
			Vector3 edge = currentPlatform.leftEdge;
			if (!onLastPlatform) {
				jumpTarget = nextConnection.connectedPlatform.rightEdge;
				if (nextConnection.connectedPlatform.platformCollider.bounds.center.x > transform.position.x) {
					edge = currentPlatform.rightEdge;
					jumpTarget = nextConnection.connectedPlatform.leftEdge;
				}
			} else {
				jumpTarget = target.position;
				if (target.position.x > transform.position.x) {
					edge = currentPlatform.rightEdge;
				}
			}
			if ( (Mathf.Abs (jumpTarget.x - transform.position.x) < currentPlatform.agentJumpDistance || Mathf.Abs (edge.x - transform.position.x) < 0.1f) && !onLastPlatform) 
			{ // Close enough to make the jump
				float jumpTime = Mathf.Max (1, Vector3.Distance (transform.position, jumpTarget) / maxSpeed);
				movementCoroutine = JumpFromTo (transform.position, jumpTarget, jumpTime);
			} 
			else if (!onLastPlatform) 
			{ // Not close enough to make the jump
				movementCoroutine = MoveTowards (jumpTarget, edge, true);
			} 
			else 
			{
				movementCoroutine = MoveTowards (target.position, edge, false);
			}
			StartCoroutine (movementCoroutine);
		}

		if (Time.time - chaseStartedTime > timeBeforeTeleport && pathHasChanged) {
			if (movementCoroutine != null) {
				StopCoroutine (movementCoroutine);
			}
			TeleportToTarget ();
		}
	}

	void TeleportToTarget(){
		pathHasChanged = false;
		path = null;
		nextConnection = null;
		FinishedAction ( false );
		ParticleSystem teleportSystem = GetComponentInChildren<ParticleSystem> ();
		if (teleportSystem != null) {
			teleportSystem.Play();
		}
		float randomLanding = Random.Range (-endMaxDistance, endMaxDistance);
		transform.position = target.position;
		PutOnGround ();
	}

	void FinishedAction( bool shouldPop, bool teleportOnNoPath = false ){
		isActioning = false;
		onLastPlatform = false;
		if (shouldPop) {
			if (path == null || path.Count <= 1) {
				nextConnection = null;
				return;
			}
			path.RemoveAt (0);
			nextConnection = path [0];
		}
	}

	void PutOnGround(){
		// Capsule cast down from the actor location and place it where it lands
		RaycastHit2D hit = Physics2D.CapsuleCast(myCollider.bounds.center + new Vector3(0, 0.1f), myCollider.size, myCollider.direction, 0, Vector2.down, height*2.0f, layerMask);
		if (hit.collider != null) {
			currentPlatform = hit.collider.gameObject.GetComponent<PathablePlatform>();
			transform.position = new Vector2 (transform.position.x, hit.point.y + height);
		} else {
			Debug.Log ("unable to place on ground");
			currentPlatform = null;
		}
	}

	IEnumerator MoveTowards(Vector3 target, Vector3 edge, bool jumpTo ){
		float endDistance = Random.Range (0, endMaxDistance);
		if (currentPlatform == null) {
			transform.position = edge;
		} else {
			while ( 
					(Mathf.Abs (target.x - transform.position.x) > currentPlatform.agentJumpDistance && Mathf.Abs (edge.x - transform.position.x) > 0.1f && jumpTo)
				||	(Mathf.Abs (target.x - transform.position.x) > endDistance && Mathf.Abs (edge.x - transform.position.x) > 0.1f && !jumpTo)
			) {
				
				curSpeed += acceleration * Time.deltaTime * Mathf.Sign (target.x - transform.position.x) * Random.Range (0.3f, 1.2f);
				curSpeed = Mathf.Min (Mathf.Max (-maxSpeed, curSpeed), maxSpeed); // Clamp speed

				Vector2 nextPos = new Vector2 (transform.position.x + (curSpeed * Random.Range (0.6f, 1.2f) * Time.deltaTime), transform.position.y);

				transform.position = nextPos;
				PutOnGround ();

				yield return null;
			}
		}
		if (!jumpTo) {
			curSpeed = 0;
		}
		FinishedAction (false);
		yield break;
	}

	IEnumerator JumpFromTo(Vector2 p0, Vector2 p2, float time){
		
		if (time <= 0) {
			Debug.LogWarning ("Attempted to make 0 second jump");
			time = 0.1f;
		}

		if ((p0.x > p2.x && curSpeed > 0) || (p0.x < p2.x && curSpeed < 0)) { // Jumping to the left, but speed to the right || jumping to the right, but speed to the left
			curSpeed = 0; // Kill speed
		}

		float startTime = Time.time;
		float jumpApex = p0.y + jumpHeight;
		if (jumpApex < p2.y) {
			jumpApex = p2.y + jumpHeight * 0.2f;
		}
		Vector2 p1 = new Vector2 (p0.x + ((p2.x - p0.x) * 0.5f), jumpApex); // Control point for the apex of the jump

		float t = 0;
		while (t < 1) {
			t = (Time.time - startTime) / time;
			if (t > 1) {
				t = 1;
			}
			float curveX = Mathf.Pow (1 - t, 2) * p0.x + 2 * (1 - t) * t * p1.x + Mathf.Pow (t, 2) * p2.x;
			float curveY = Mathf.Pow (1 - t, 2) * p0.y + 2 * (1 - t) * t * p1.y + Mathf.Pow (t, 2) * p2.y;

			transform.position = new Vector3 (curveX, curveY + height, 0);
			yield return null;
		}

		PutOnGround ();
		FinishedAction (true);
		yield break;
	}

	IEnumerator FindPath(){

		if (currentPlatform == null) { // Not on a pathable platform
			yield break;
		}
		if (targetPlatform == null) {
			yield break;
		}

		platformsConsidered.Clear ();

		PathablePlatform.PlatformConnection tmp = new PathablePlatform.PlatformConnection (PathablePlatform.ConnectionType.EDGE, currentPlatform);
		path = RecursivePathSearch (tmp, targetPlatform, Time.time);

		if (path == null ) {
			TeleportToTarget ();
		}

		FinishedAction (true, true);
	}

	public List<PathablePlatform.PlatformConnection> RecursivePathSearch(PathablePlatform.PlatformConnection start, PathablePlatform targ, float startTime){
		if (platformsConsidered.Exists (x => x == start.connectedPlatform) || Time.time - startTime > maxPathingTime / 1000) { // Check if this platform has already been explored, or if we've timed out
			return null;
		}
		platformsConsidered.Add (start.connectedPlatform);

		List<PathablePlatform.PlatformConnection> returnList = new List<PathablePlatform.PlatformConnection> ();
		returnList.Add (start);

		if (start.connectedPlatform == targ) { // Check if this platform is the target
			return returnList;
		}

		List<PathablePlatform.PlatformConnection> connections = start.connectedPlatform.connections.OrderBy ( item => {
			// Put all platforms which are between me and the target of equal high priority
			if( Mathf.Sign( targ.platformCollider.bounds.center.x - myCollider.bounds.center.x) == Mathf.Sign( item.connectedPlatform.platformCollider.bounds.center.x - myCollider.bounds.center.x) ){
				return 0;
			}else{
				return 1;
			}
		}).ThenBy( item => {
			// Choose the closest platform to self to find the path
			return Vector3.Distance (item.connectedPlatform.platformCollider.bounds.center, myCollider.bounds.center);
		}).ToList();

		for (int i = 0; i < connections.Count; i++) {
			List<PathablePlatform.PlatformConnection> subPath = RecursivePathSearch (connections [i], targ, startTime); // Find the target in the children
			if (subPath != null) {
				subPath.Insert (0, start);
				return subPath;
			}
		}
		return null;
	}
}
