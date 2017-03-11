using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PathablePlatform : MonoBehaviour {

	[Header("Pathing parameters")]
	[Tooltip("How high can the agent jump. Do they got mad ups?")]
	public float agentJumpHeight = 3f;
	[Tooltip("How far forward can the agent jump from the ground")]
	public float agentJumpDistance = 4f;
	[Tooltip("How far the agent can move laterally for each unit it falls")]
	public float agentFloatDistance = 0.2f;
	[Tooltip("How far can the agent drop before it shouldn't be considered a valid move")]
	public float agentDropHeight = 10f;
	[Tooltip("Number of world units per pathfinding grid unit")]
	public float gridUnit = 0.5f;
	[Tooltip("How big is the agent traversing this platform")]
	public Vector2 agentBounds = new Vector2(0.5f,1f);

	[Tooltip("Show the pathing gizmos")]
	public bool showPathingData = true;

	[System.Serializable]
	public enum ConnectionType{
		EDGE,
		LEAP,
		DROP_THROUGH
	}

	[System.Serializable]
	public class PlatformConnection
	{
		public ConnectionType connectionType;
		public PathablePlatform connectedPlatform;

		public PlatformConnection(ConnectionType type, PathablePlatform platform){
			connectionType = type;
			connectedPlatform = platform;
		}
	}

	[SerializeField]
	public List<PlatformConnection> connections = new List<PlatformConnection>();

	public Collider2D platformCollider;
	private Vector3 oobBounds;

	public Vector2 leftEdge;
	public Vector2 rightEdge;

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
		if (!showPathingData) {
			return;
		}

		platformCollider = GetComponent<Collider2D> ();
		if (platformCollider == null) {
			Debug.LogError ("Platform was unable to obtain a reference to it's collider");
		}

		if (Time.time - 0.1 > lastUpdateTime) {
			lastUpdateTime = Time.time;
			FindConnectedPlatforms ();
		}

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere (leftEdge, 0.2f);
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere (rightEdge, 0.2f);

		for (int i = 0; i < connections.Count; i++) {
			Gizmos.color = Color.red;
			Gizmos.DrawLine (transform.position, connections [i].connectedPlatform.gameObject.transform.position);
		}



		if (gridUnit <= 0) {
			return;
		}
		Gizmos.color = new Color (0, 0, 1, 0.2f);//Color.blue;
		// short and low -> long and low -> short and high -> long and high
		float rightSearchWidth = agentJumpDistance;
		float leftSearchWidth = -agentJumpDistance;
		for (float y = agentJumpHeight; y > -agentDropHeight; y -= gridUnit) {
			for (float x = leftSearchWidth; Mathf.Abs(x) <= rightSearchWidth; x += gridUnit) {
				Vector2 origin = rightEdge + (new Vector2 (x + gridUnit, y - gridUnit));
				Gizmos.DrawCube (origin, new Vector3 (gridUnit, gridUnit, gridUnit));
			}
			rightSearchWidth += agentFloatDistance;
			if (y > gridUnit) {
				leftSearchWidth += agentFloatDistance;
			} else if (y > 0) {
				leftSearchWidth = 0;
			} else if (y < -gridUnit) {
				leftSearchWidth -= agentFloatDistance;
			}
		}

		rightSearchWidth = agentJumpDistance;
		leftSearchWidth = agentJumpDistance;
		for (float y = agentJumpHeight; y > -agentDropHeight; y -= gridUnit) {
			for (float x = leftSearchWidth; Mathf.Abs(x) <= rightSearchWidth; x -= gridUnit) {
				Vector2 origin = leftEdge + (new Vector2 (x - gridUnit, y - gridUnit));
				Gizmos.DrawCube (origin, new Vector3 (gridUnit, gridUnit, gridUnit));
			}
			rightSearchWidth += agentFloatDistance;
			if (y > gridUnit) {
				leftSearchWidth -= agentFloatDistance;
			} else if (y > 0) {
				leftSearchWidth = 0;
			} else if (y < -gridUnit) {
				leftSearchWidth += agentFloatDistance;
			}
		}
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
			leftEdge = edgeCollider.points[0] + new Vector2(0.2f,0);
			rightEdge = edgeCollider.points [edgeCollider.pointCount - 1]  - new Vector2(0.2f,0);
		} else {
			leftEdge = new Vector2 (platformCollider.bounds.min.x - platformCollider.gameObject.transform.position.x + 0.2f, platformCollider.bounds.max.y - platformCollider.gameObject.transform.position.y + 0.1f);
			rightEdge = new Vector2 (platformCollider.bounds.max.x - platformCollider.gameObject.transform.position.x - 0.2f, platformCollider.bounds.max.y - platformCollider.gameObject.transform.position.y + 0.1f);
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
					AddPlatformConnection (platform, ConnectionType.LEAP);
				}
			}
		}

		// Find platforms off the left edge
		GetEdgePlatforms(leftEdge, Vector2.left);
		// Find platforms off the right edge
		GetEdgePlatforms(rightEdge, Vector2.right);
	}

	protected void AddPlatformConnection(PathablePlatform platform, ConnectionType type){
		// Check if this platform is self, then don't add
		if (platform == this) {
			return;
		}
		// Check if this platform has already been added to the list
		if (connections.Exists (x => x.connectedPlatform == platform)) {
			return;
		}
		PlatformConnection conneciton = new PlatformConnection (type, platform);
		connections.Add (conneciton);
	}

	public void GetEdgePlatforms(Vector2 edge, Vector2 dir){
		if (gridUnit <= 0) {
			return;
		}


		// short and low -> long and low -> short and high -> long and high
		float rightSearchWidth = agentJumpDistance;
		float leftSearchWidth = -agentJumpDistance * Mathf.Sign(dir.x);
		for (float y = agentJumpHeight; y > -agentDropHeight; y -= gridUnit) {
			for (float x = leftSearchWidth; Mathf.Abs(x) <= rightSearchWidth; x += gridUnit * Mathf.Sign(dir.x)) {
				Vector2 origin = edge + (new Vector2 (x + (gridUnit * Mathf.Sign(dir.x)), y - gridUnit + gridUnit/2.0f));
				RaycastHit2D[] hits = Physics2D.RaycastAll (origin, Vector2.down, agentDropHeight - y);
				if (hits.Length > 0) {
					for (int i = 0; i < hits.Length; i++) {
						PathablePlatform platform = hits[i].collider.gameObject.GetComponent<PathablePlatform>();
						if (platform != null) {
							AddPlatformConnection (platform, ConnectionType.EDGE);
						}
					}
				}
			}
			rightSearchWidth += agentFloatDistance;
			if (y > gridUnit) {
				leftSearchWidth += agentFloatDistance * Mathf.Sin(dir.x);
			} else if (y > 0) {
				leftSearchWidth = 0;
			} else if (y < -gridUnit) {
				leftSearchWidth -= agentFloatDistance * Mathf.Sin(dir.x);
			}
		}
	}
}
