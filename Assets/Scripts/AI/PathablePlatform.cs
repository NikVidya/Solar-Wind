using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PathablePlatform : MonoBehaviour {

	[Header("Pathing parameters")]
	[Tooltip("How high can the agent jump. Do they got mad ups?")]
	public float agentJumpHeight = 2f;
	[Tooltip("How far forward can the agent jump from the ground")]
	public float agentJumpDistance = 2f;
	[Tooltip("How far the agent can move laterally for each unit it falls")]
	public float agentFloatDistance = 1f;
	[Tooltip("How far can the agent drop before it shouldn't be considered a valid move")]
	public float agentDropHeight = 4f;
	[Tooltip("Number of world units per pathfinding grid unit")]
	public float gridUnit = 0.5f;
	[Tooltip("How big is the agent traversing this platform")]
	public Vector2 agentBounds = new Vector2(0.5f,1f);

	public enum ConnectionType{
		WALK,
		JUMP,
		DROP
	}
	public class PlatformConnection
	{
		public ConnectionType connectionType;
		public PathablePlatform connectedPlatform;

		public PlatformConnection(ConnectionType type, PathablePlatform platform){
			connectionType = type;
			connectedPlatform = platform;
		}
	}
	private List<PlatformConnection> connections = new List<PlatformConnection>();
	private Collider2D platformCollider;
	private Vector3 oobBounds;

	private Vector2 leftEdge;
	private Vector2 rightEdge;

	void Awake(){
		platformCollider = GetComponent<Collider2D> ();
		if (platformCollider == null) {
			Debug.LogError ("Platform was unable to obtain a reference to it's collider");
		}

		// Find platforms that can be reached from here
		FindConnectedPlatforms ();
	}

	#if UNITY_EDITOR
	private float lastUpdateTime;
	void OnDrawGizmosSelected(){
		if (Time.time - 0.1 > lastUpdateTime) {
			lastUpdateTime = Time.time;
			FindConnectedPlatforms ();
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere (leftEdge, 0.2f);
		Gizmos.DrawWireSphere (rightEdge, 0.2f);

		for (int i = 0; i < connections.Count; i++) {
			Gizmos.color = Color.red;
			Gizmos.DrawLine (transform.position, connections [i].connectedPlatform.gameObject.transform.position);
		}



		if (gridUnit <= 0) {
			return;
		}
		Gizmos.color = Color.blue;
		// short and low -> long and low -> short and high -> long and high
		float searchWidth = agentJumpDistance;
		for (float y = 0; y <= agentJumpHeight; y += gridUnit) {
			for (float x = 0; Mathf.Abs(x) <= searchWidth; x -= gridUnit) {
				Vector2 origin = leftEdge + (new Vector2 (x + (gridUnit * -0.5f), y - gridUnit * 0.5f));
				Gizmos.DrawCube (origin, new Vector3 (gridUnit, gridUnit, gridUnit) * 0.5f);
				//Debug.DrawRay (origin, new Vector2(0,-0.2f), Color.blue, 10);
			}
			searchWidth += agentFloatDistance;
		}

		// short and low -> long and low -> short and high -> long and high
		/*searchWidth = agentJumpDistance;
		for (float y = 0; y <= agentJumpHeight; y += gridUnit) {
			for (float x = 0; Mathf.Abs(x) <= searchWidth; x += gridUnit) {
				Vector2 origin = rightEdge + (new Vector2 (x + (gridUnit * -0.5f), y - gridUnit * 0.5f));
				Gizmos.DrawCube (origin, new Vector3 (gridUnit, gridUnit, gridUnit) * 0.5f);
				//Debug.DrawRay (origin, new Vector2(0,-0.2f), Color.blue, 10);
			}
			searchWidth -= agentFloatDistance;
		}*/

	}
	#endif

	void FindConnectedPlatforms(){
		connections.Clear ();

		// Get the edges of the platform
		EdgeCollider2D edgeCollider = platformCollider as EdgeCollider2D;
		Quaternion rot = transform.rotation;
		transform.rotation = Quaternion.identity;
		if (edgeCollider != null) {
			// Get the first and last points as the edges
			leftEdge = edgeCollider.points[0];
			rightEdge = edgeCollider.points [edgeCollider.pointCount - 1];
		} else {
			leftEdge = new Vector2 (-platformCollider.bounds.size.x, platformCollider.bounds.max.y - platformCollider.gameObject.transform.position.y) * 0.5f;
			rightEdge = new Vector2 (platformCollider.bounds.size.x, platformCollider.bounds.max.y - platformCollider.gameObject.transform.position.y) * 0.5f;
		}
		oobBounds = platformCollider.bounds.extents * 2f;
		if (oobBounds.y < 0.1f) {
			oobBounds.y = 0.1f;
		}
		transform.rotation = rot;
		leftEdge = transform.TransformPoint (leftEdge);
		rightEdge = transform.TransformPoint (rightEdge);


		// Cast the platform up to find platforms above
		// Player can't jump up through platforms, neither should the npc
		Vector2 origin = platformCollider.bounds.center;
		float dist = agentJumpHeight;
		RaycastHit2D[] hits = Physics2D.BoxCastAll (transform.position, oobBounds, transform.rotation.eulerAngles.z, Vector2.up, dist);
		if (hits.Length > 0) {
			for (int i = 0; i < hits.Length; i++) {
				PathablePlatform platform = hits [i].collider.gameObject.GetComponent<PathablePlatform> ();
				if (platform != null) {
					PlatformConnection connection = new PlatformConnection (ConnectionType.JUMP, platform);
					connections.Add (connection);
				}
			}
		}

		// Find platforms off the left edge
		GetEdgePlatforms(leftEdge, Vector2.left);
		// Find platforms off the right edge
		GetEdgePlatforms(rightEdge, Vector2.right);
	}

	public void GetEdgePlatforms(Vector2 edge, Vector2 dir){
		if (gridUnit <= 0) {
			return;
		}
		// short and low -> long and low -> short and high -> long and high
		float searchWidth = agentJumpDistance;
		/*for (float y = 0; y <= agentJumpHeight; y += gridUnit) {
			for (float x = 0; Mathf.Abs(x) <= searchWidth; x += gridUnit * dir.x) {
				Vector2 origin = edge + (new Vector2 (x + (gridUnit * 0.5f * dir.x), y - gridUnit * 0.5f));
				Debug.DrawRay (origin, new Vector2(0, - (agentDropHeight - y)), Color.blue, 10);
				RaycastHit2D[] hits = Physics2D.RaycastAll (origin, Vector2.down, agentDropHeight - y);//Physics2D.BoxCastAll (origin, agentBounds, 0, Vector2.down, agentDropHeight);
				if (hits.Length > 0) {
					for (int i = 0; i < hits.Length; i++) {
						//Debug.LogFormat ("Boxcast hit object: {0} from {1}", hits [0].collider.name, name);
						PathablePlatform platform = hits[i].collider.gameObject.GetComponent<PathablePlatform>();
						if (platform != null) {
							connections.Add (new PlatformConnection (ConnectionType.JUMP, platform));
						}
					}
				}
			}
			searchWidth -= agentFloatDistance;
		}*/

		/*for (float y = edge.y; y < agentJumpHeight; y += gridUnit) {
			Debug.DrawRay (edge + new Vector2 (0, y), Vector2.left);
			for (float x = edge.x; x < agentJumpDistance; x += gridUnit) {
				Debug.DrawRay (edge + new Vector2 (x, y), Vector2.down);
				// Cast a ray down to find a platform under the position
				RaycastHit2D hit = Physics2D.BoxCast(edge + new Vector2(x, y), agentBounds, 0, Vector2.down);
				if (hit.collider != null) {
					//Debug.LogFormat ("Found a platform under the jump {0}, {1} for object {2}", x, y, name);
					PathablePlatform platform = hit.collider.gameObject.GetComponent<PathablePlatform>();
					if (platform != null) {
						connections.Add (new PlatformConnection (ConnectionType.JUMP, platform));
					}
				}
			}
		}*/
	}

	private void drawCube( Vector2 origin, Vector2 size, Color col, float dur){
		Debug.DrawLine (origin, origin + new Vector2 (size.x, 0), col, dur);
		Debug.DrawLine (origin + new Vector2 (size.x, 0), origin + new Vector2 (size.x, size.y), col, dur);
		Debug.DrawLine (origin + new Vector2 (size.x, size.y), origin + new Vector2 (0, size.y), col, dur);
	}
}
