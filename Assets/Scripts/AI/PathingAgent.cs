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

	private CapsuleCollider2D myCollider;
	private List<PathablePlatform.PlatformConnection> path;
	private List<PathablePlatform> platformsConsidered = new List<PathablePlatform>();
	private int layerMask = 1 << 8;
	IEnumerator pathFindingCoroutine;

	void Start () {
		myCollider = GetComponent<CapsuleCollider2D> ();
		if (myCollider == null) {
			Debug.LogError ("Pathing Agent needs a capsule collider 2d");
			return;
		}
	}

	float lastPathTime;
	Vector3 lastPathPos;
	void Update(){

		if (Time.time - lastPathTime > 1 && Vector3.Distance(lastPathPos, transform.position) > 0.5f) {
			lastPathTime = Time.time;
			lastPathPos = transform.position;
			// Put the agent on the ground
			RaycastHit2D hit = Physics2D.CapsuleCast(myCollider.bounds.center + new Vector3(0,height*2,0), myCollider.size, myCollider.direction, transform.rotation.eulerAngles.z, Vector2.down, Mathf.Infinity, layerMask);
			if ( hit.collider != null) {
				transform.position = hit.point + new Vector2(0, height);
			}

			if (pathFindingCoroutine != null) {
				StopCoroutine (pathFindingCoroutine);
			}
			pathFindingCoroutine = FindPath ();
			StartCoroutine (pathFindingCoroutine);
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

		path = RecursivePathSearch (new PathablePlatform.PlatformConnection(PathablePlatform.ConnectionType.EDGE, startPlatform), targetPlatform, Time.time);
		if (path == null) {
			Debug.LogWarning ("Could not find path");
			yield break;
		}
		PathablePlatform.PlatformConnection[] temp = path.ToArray ();
		for (int i = 0; i < temp.Length-1; i++) {
			Debug.DrawLine (temp [i].connectedPlatform.transform.position, temp [i + 1].connectedPlatform.transform.position, Color.red, 5);
		}
	}

	public List<PathablePlatform.PlatformConnection> RecursivePathSearch(PathablePlatform.PlatformConnection start, PathablePlatform targ, float startTime){
		if (platformsConsidered.Exists (x => x == start.connectedPlatform) || Time.time - startTime > maxPathingTime) { // Check if this platform has already been explored, or if we've timed out
			return null;
		}
		platformsConsidered.Add (start.connectedPlatform);

		List<PathablePlatform.PlatformConnection> returnList = new List<PathablePlatform.PlatformConnection> ();
		returnList.Add (start);

		if (start.connectedPlatform == targ) { // Check if this platform is the target
			return returnList;
		}
		List<PathablePlatform.PlatformConnection> bestPath = null;
		for (int i = 0; i < start.connectedPlatform.connections.Count; i++) {
			List<PathablePlatform.PlatformConnection> subPath = RecursivePathSearch (start.connectedPlatform.connections [i], targ, startTime); // Find the target in the children
			if (subPath != null) {
				string tmp = "Potential Path: ";
				for (int j = 0; j < subPath.Count; j++) {
					tmp += subPath [j].connectedPlatform.gameObject.name + " -> ";
				}
				Debug.Log (tmp);
				if (bestPath == null || subPath.Count < bestPath.Count) {
					bestPath = subPath;
				}
			}
		}
		if (bestPath != null) {
			returnList.AddRange (bestPath); // Add the best path from this connection to the list
			return returnList;
		}
		return null;
	}
}
