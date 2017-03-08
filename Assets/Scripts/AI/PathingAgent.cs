using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CapsuleCollider2D))]
public class PathingAgent : MonoBehaviour {

	[Header("Agent Movement Properties")]
	[Tooltip("Max travel speed in units/second")]
	public float maxSpeed = 2;
	[Tooltip("Acceleration rate in units/second^2")]
	public float acceleration = 0.5f;
	[Tooltip("Maximum unit height of jump")]
	public float jumpHeight = 3;

	[Header("Agent Pathfinding Properties")]
	[Tooltip("Height of the agent")]
	public float height = 0.5f;
	[Tooltip("Length of time to spend moving towards target (in seconds)")]
	public float maxMovingTime = 3f; // In seconds
	[Tooltip("Length of time to spend pathfinding (in milliseconds) (keep small to avoid framedrops)")]
	public float maxPathingTime = 20f; // In milliseconds

	public Transform target;

	private CapsuleCollider2D myCollider;
	private PathablePlatform currentPlatform;
	private List<PathablePlatform.PlatformConnection> path;
	private List<PathablePlatform> platformsConsidered = new List<PathablePlatform>();
	private int layerMask = 1 << 8;
	IEnumerator pathFindingCoroutine;
	IEnumerator movementCoroutine;
	PathablePlatform.PlatformConnection nextConnection = null;

	float lastPathTime;
	Vector3 oldTargetPos;
	bool isMoving = false;

	float curSpeed;
	float jumpStartTime;
	float jumpMaxTime;

	void Start () {
		myCollider = GetComponent<CapsuleCollider2D> ();
		if (myCollider == null) {
			Debug.LogError ("Pathing Agent needs a capsule collider 2d");
			return;
		}
		// Put the agent on the ground
		RaycastHit2D hit = Physics2D.CapsuleCast(myCollider.bounds.center + new Vector3(0,height*2,0), myCollider.size, myCollider.direction, transform.rotation.eulerAngles.z, Vector2.down, Mathf.Infinity, layerMask);
		if ( hit.collider != null) {
			currentPlatform = hit.collider.gameObject.GetComponent<PathablePlatform> ();
			if (currentPlatform == null) {
				Debug.LogWarning ("NPC agent is not on a pathable surface");
			}
		}
	}

	void Update(){

		if (Time.time - lastPathTime > 1 &&  Vector3.Distance(oldTargetPos, target.position) > 0.1f) {
			if (path != null) {
				path.Clear ();
			}
			nextConnection = null;
			lastPathTime = Time.time;
			oldTargetPos = target.position;

			if (pathFindingCoroutine != null) {
				StopCoroutine (pathFindingCoroutine);
			}
			pathFindingCoroutine = FindPath ();
			StartCoroutine (pathFindingCoroutine);
		}

		if (path != null && path.Count > 0) {
			if (nextConnection == null) {
				nextConnection = path [0]; // Pulled a new step from the path
				jumpStartTime = -1;
				jumpMaxTime = -1;
			}
			// Move to a point on the platform
			Vector3 moveTarget;
			Vector3 jumpTarget;
			switch (nextConnection.connectionType){
			case PathablePlatform.ConnectionType.EDGE:
				if (nextConnection.connectedPlatform.platformCollider.bounds.center.x > currentPlatform.platformCollider.bounds.center.x) {
					moveTarget = currentPlatform.rightEdge;
					jumpTarget = nextConnection.connectedPlatform.leftEdge;
				} else {
					moveTarget = currentPlatform.leftEdge;
					jumpTarget = nextConnection.connectedPlatform.rightEdge;
				}
				break;
			case PathablePlatform.ConnectionType.LEAP:
				if (nextConnection.connectedPlatform.platformCollider.bounds.center.x > currentPlatform.platformCollider.bounds.center.x) {
					jumpTarget = nextConnection.connectedPlatform.leftEdge;
				} else {
					jumpTarget = nextConnection.connectedPlatform.rightEdge;
				}
				// Get movement target
				RaycastHit2D hit = Physics2D.Raycast (jumpTarget - new Vector3 (0, height), Vector2.down, Mathf.Infinity, layerMask);
				if (hit != null && hit.collider != null) {
					moveTarget = hit.point;
				} else {
					moveTarget = jumpTarget;
				}
				break;
			default:
				moveTarget = jumpTarget = transform.position;
				break;
			}

			Debug.DrawLine (transform.position, moveTarget, Color.cyan, 1);
			Debug.DrawLine (currentPlatform.platformCollider.bounds.center, nextConnection.connectedPlatform.platformCollider.bounds.center, Color.blue, 1);

			if (transform.position.x < moveTarget.x && Mathf.Abs(transform.position.x - moveTarget.x) > 0.1f && jumpStartTime < 0) 
			{
				MoveDirection (Vector2.right);
			} 
			else if(transform.position.x > moveTarget.x && Mathf.Abs(transform.position.x - moveTarget.x) > 0.1f && jumpStartTime < 0) 
			{
				MoveDirection (Vector2.left);
			} 
			else if(Mathf.Abs(transform.position.x - moveTarget.x) < 0.1f || jumpStartTime > 0) 
			{
				// Jump to the next point

				if (jumpMaxTime < 0) {
					switch(nextConnection.connectionType){
					case PathablePlatform.ConnectionType.EDGE:
						jumpMaxTime = Vector3.Distance(moveTarget, jumpTarget) / curSpeed; // Amount of time it would take to travel that distance at current speed
						break;
					case PathablePlatform.ConnectionType.LEAP:
						jumpMaxTime = 1.0f; // Flat time
						break;
					}
					Debug.LogFormat ("Jumping over time {0}", jumpMaxTime);
				}
				if (jumpStartTime < 0) {
					jumpStartTime = Time.time;
				}

				JumpFromTo(moveTarget, jumpTarget);
			}
		}
		if (jumpStartTime < 0) {
			GroundAgent ();
		}
	}

	void GroundAgent(){
		RaycastHit2D hit = Physics2D.Raycast(transform.position + new Vector3(0, height), Vector2.down, Mathf.Infinity, layerMask);
		if (hit != null && hit.collider != null) {
			transform.position = hit.point + new Vector2(0, height);
			currentPlatform = hit.collider.gameObject.GetComponent<PathablePlatform> ();
			if (currentPlatform == null) {
				Debug.LogWarning ("NPC agent is not on a pathable surface");
			}
		}
	}

	void MoveDirection(Vector2 dir){
		curSpeed += acceleration * Mathf.Sign (dir.x) * Time.deltaTime;
		if (curSpeed > maxSpeed) {
			curSpeed = maxSpeed;
		} else if(curSpeed < -maxSpeed) {
			curSpeed = -maxSpeed;
		}

		Vector3 targetPos = transform.position + new Vector3(curSpeed * Time.deltaTime, 0, 0);
		transform.position = targetPos;
		// Find that position on the ground
		Debug.DrawLine(transform.position, targetPos, Color.red, 1);
	}

	void JumpFromTo(Vector3 p0, Vector3 p2){
		if (jumpMaxTime <= 0) {
			Debug.LogWarning ("Attempted to make jump with max time of 0");
			jumpMaxTime = 0.1f;
		}
		float t = (Time.time - jumpStartTime) / jumpMaxTime;
		float jumpApex = p0.y + jumpHeight;
		if (jumpApex < p2.y) {
			jumpApex = p2.y + jumpHeight * 0.2f;
		}
		Vector2 p1 = new Vector2 (p0.x + ((p2.x - p0.x) * 0.5f), jumpApex); // Control point for the apex of the jump
		Debug.DrawLine(p0,p1, Color.white, 1);
		Debug.DrawLine(p1,p2, Color.magenta, 1);

		float curveX = Mathf.Pow (1 - t, 2) * p0.x + 2 * (1 - t) * t * p1.x + Mathf.Pow (t, 2) * p2.x;
		float curveY = Mathf.Pow (1 - t, 2) * p0.y + 2 * (1 - t) * t * p1.y + Mathf.Pow (t, 2) * p2.y;

		transform.position = new Vector3 (curveX, curveY + height, 0);

		if (t >= 1) { // We finished the jump, next node
			jumpMaxTime = -1;
			jumpStartTime = -1;
			path.RemoveAt (0);
			nextConnection = null;
		}
	}

	IEnumerator FindPath(){
		// Get the target platform to move to
		RaycastHit2D hit = Physics2D.Raycast(target.transform.position + new Vector3(0,1,0), Vector2.down, Mathf.Infinity, layerMask);
		if ( hit.collider == null) {
			yield break; // No path to this location
		}
		PathablePlatform targetPlatform = hit.collider.gameObject.GetComponent<PathablePlatform>();
		if (targetPlatform == null) {
			Debug.Log ("No script");
			yield break; // Can't path to a platform that isn't pathable
		}

		// Get the platform to start from
		hit = Physics2D.Raycast(transform.position + new Vector3(0,1,0), Vector2.down, Mathf.Infinity, layerMask);
		if ( hit.collider == null) {
			yield break; // Not on a platform
		}
		PathablePlatform startPlatform = hit.collider.gameObject.GetComponent<PathablePlatform>();
		if (startPlatform == null) {
			yield break; // Not on pathable platform
		}

		platformsConsidered.Clear ();

		PathablePlatform.PlatformConnection tmp = new PathablePlatform.PlatformConnection (PathablePlatform.ConnectionType.EDGE, startPlatform);
		path = RecursivePathSearch (tmp, targetPlatform, Time.time);
		if (path == null) {
			Debug.LogWarning ("Could not find path");
			yield break;
		} else {
			path.Remove (tmp);
		}
		for (int i = 0; i < path.Count - 1; i++) {
			Debug.DrawLine (path [i].connectedPlatform.platformCollider.bounds.center, path [i + 1].connectedPlatform.platformCollider.bounds.center, Color.green, 3);
		}
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

		// Get a new list and depth sort by distance to target
		List<PathablePlatform.PlatformConnection> connections = start.connectedPlatform.connections.OrderBy (item => {
			// if the platform is between player and the target, highest priority
			Vector3 checkPos = item.connectedPlatform.platformCollider.bounds.center;
			if(Mathf.Sign( checkPos.x - transform.position.x) == Mathf.Sign(targ.platformCollider.bounds.center.x - transform.position.x)){
				// Both the target and this platform are on the same side of the agent, they must be in a line to it
				return 1;
			}else{
				return 0;
			}
		}).ThenBy( item => {
			return Vector3.Distance(targ.platformCollider.bounds.center, item.connectedPlatform.platformCollider.bounds.center);
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
