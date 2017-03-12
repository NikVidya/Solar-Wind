using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CapsuleCollider2D))]
public class PathingAgent : MonoBehaviour
{
	[Header("Agent Abilities")]
	[Tooltip("Distance the agent travels during a dash in units")]
	public float dashDistance = 3.0f;
	[Tooltip("Distance the agent can travel laterally while falling per unit in units")]
	public float floatDistance = 0.5f;
	[Tooltip("Height the agent can jump in units")]
	public float jumpHeight = 3.0f;

	[Header("Agent Pathfinding Properties")]
	[Tooltip("Delay (seconds) before the agent will start following a new path")]
	public float pathingDelay = 1f;
	[Tooltip("Seconds to chase the player before teleporting")]
	public float secondsBeforeTeleport = 5;
	[Tooltip("Seconds to spend finding a path to the player before giving up")]
	public float maxPathfindingTime = 3;
	[Tooltip("Seconds to spend per tick finding a path to the player")]
	public float maxPerTickPathfindingTime = 0.01f;
	[Tooltip("How far the target has to move before new path will be produced")]
	public float targetDistanceTollerance = 0.5f;
	[Tooltip("How many times in a row the agent can pick the DROP action before it will be suppressed")]
	public int maxDropCount = 4;
	[Tooltip("How many cells can the agent climb per move")]
	public int maxClimbCount = 2;

	[Header("Details")]
	public Transform target;

	PlatformerNavMesh navmesh;
	CapsuleCollider2D myCollider;

	Vector2 position {
		get {
			return myCollider.bounds.center;
		}
		set {
			transform.position = value - ((Vector2)myCollider.bounds.center - (Vector2)transform.position);
		}
	}

	public enum AgentAction {
		MOVE_LEFT,
		MOVE_RIGHT,
		FLOAT_LEFT,
		FLOAT_RIGHT,
		DASH_LEFT,
		DASH_RIGHT,
		JUMP,
		JUMP_CONTINUE,
		DROP,
		DROP_LEFT,
		DROP_RIGHT,
		LAND
	}
	// LUT for which actions can follow other actions (i.e. jump can't follow jump)
	public Dictionary<AgentAction, List<AgentAction>> followUpActions = new Dictionary<AgentAction, List<AgentAction>>
	{
		{ AgentAction.MOVE_LEFT, 		new List<AgentAction>(){AgentAction.MOVE_LEFT, 	AgentAction.JUMP, 			AgentAction.DROP,		AgentAction.DROP_LEFT} },
		{ AgentAction.MOVE_RIGHT, 		new List<AgentAction>(){AgentAction.MOVE_RIGHT, AgentAction.JUMP, 			AgentAction.DROP,		AgentAction.DROP_RIGHT} },
		{ AgentAction.DROP_RIGHT, 		new List<AgentAction>(){AgentAction.DROP,		AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_RIGHT,	AgentAction.LAND } 	},
		{ AgentAction.DROP_LEFT, 		new List<AgentAction>(){AgentAction.DROP,		AgentAction.FLOAT_LEFT, 	AgentAction.DASH_LEFT,	AgentAction.LAND } 	},
		{ AgentAction.FLOAT_LEFT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND} 			},
		{ AgentAction.FLOAT_RIGHT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND} 			},
		{ AgentAction.JUMP, 			new List<AgentAction>(){AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_LEFT, 	AgentAction.DASH_RIGHT, AgentAction.LAND, AgentAction.JUMP_CONTINUE} },
		{ AgentAction.JUMP_CONTINUE, 	new List<AgentAction>(){AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_LEFT, 	AgentAction.DASH_RIGHT, AgentAction.LAND, AgentAction.JUMP_CONTINUE} },
		{ AgentAction.DROP, 			new List<AgentAction>(){AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_LEFT, 	AgentAction.DASH_RIGHT, AgentAction.DROP, AgentAction.LAND} },
		{ AgentAction.LAND, 			new List<AgentAction>(){AgentAction.MOVE_LEFT, 	AgentAction.MOVE_RIGHT, 	AgentAction.DROP, 		AgentAction.JUMP} },
		{ AgentAction.DASH_LEFT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND} 			},
		{ AgentAction.DASH_RIGHT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND} 			},
	};
	public class CellAction {
		public PlatformerNavMesh.CellPosition start, end;
		public AgentAction action;
		public List<CellAction> nextActions;
		public int nextActionIndex;
	}
	LinkedList<CellAction> path = new LinkedList<CellAction> (); // The path for the NPC to take
	CellAction currentAction;

	bool shouldGeneratePath = true;

	/*
	 * 
	 * The path works using the producer-consumer model
	 * The coroutine is the producer (maybe even make that another thread?)
	 * The agent is the consumer
	 * 
	 * As soon as there is a valid reason to continue producing more path (i.e. the target moved), the producer will start to find a new path from wherever it left of.
	 * It will perform a depth first search.
	 * If there are no valid actions after the last action, remove the last action
	 * On the next iteration, continue from the new tail
	 * 
	 * Meanwhile, the consumer (the agent) will be fufilling the actions. Possibly over several frames
	 * Once the consumer starts an action, it can not be preempted.
	 * As soon as the consumer starts an action, it will remove it from the path, and store it in a seperate variable
	 * 
	 * On the producer side, if it goes to find a path from the tail of the path and there are no entries in the path
	 * Use the current action as the start point
	 * If the current action is null, meaning the agent has completed whatever action they had just started, but had not started another action
	 * then it's time to start a whole new path
	 * 
	 * There might be a problem with cycles
	 * It's possible that the pathfinding might end up back on a path it has already considered. If this is the case, it will simply follow the same path exactly, over and over
	 * However, I will allow this action, as the teleportation timeout will still allow the agent to reach it's destination, and break it out of the cycle
	 * 
	 * */

	IEnumerator pathProducer;
	void Start(){
		navmesh = GameObject.FindObjectOfType<PlatformerNavMesh> ();
		if (navmesh == null) {
			Debug.LogWarning ("Pathing agent found no nav mesh. Can't find paths!");
		}

		myCollider = GetComponent<CapsuleCollider2D> ();
		if (myCollider == null) {
			Debug.LogError ("PathingAgent did not have a capsule collider 2D!");
		}

		if (pathProducer != null) {
			StopCoroutine (pathProducer);
		}
		pathProducer = ProducePath ();
		StartCoroutine (pathProducer);
	}

	IEnumerator ProducePath(){
		Debug.Log ("Starting up producer");
		if (navmesh == null) {
			yield break;
		}

		int dropActionCount = 0; // How many times has the agent done DROP
		int jumpActionCount = 0; // How many times has the agent done JUMP_CONTINUE

		float lastYieldTime = Time.realtimeSinceStartup;
		float time = Time.time;
		List<AgentAction> blockedActions = new List<AgentAction> ();
		while (shouldGeneratePath) {
			/*if (Time.time - time < 0.2f) {
				yield return null;
				continue;
			}*/
			time = Time.time;
			//Debug.LogFormat ("Loop: {0}, {1}, {2}, {3}", Time.realtimeSinceStartup, lastYieldTime, Time.realtimeSinceStartup - lastYieldTime, maxPerTickPathfindingTime);
			if (Time.realtimeSinceStartup - lastYieldTime > maxPerTickPathfindingTime) {
				//Debug.Log ("Yielding to main tick");
				lastYieldTime = Time.realtimeSinceStartup;
				yield return null; // Give the main tick a chance to run
			}
			// Check if the path needs generating
			if (path.Count > 0 && Vector2.Distance (target.position, new Vector2 (path.Last.Value.end.x, path.Last.Value.end.y)) < targetDistanceTollerance) {
				yield return null;
				continue; // Don't need to produce more path
			}

			// Get the action we are continuing from
			CellAction continueFrom = null;
			if (path.Last != null) {
				continueFrom = path.Last.Value;
			}

			if (continueFrom == null) {
				continueFrom = currentAction;
			}

			if (continueFrom == null) {
				Debug.Log ("Making up a node");
				// Make one up. The actor just landed
				continueFrom = new CellAction();
				continueFrom.start = navmesh.WorldToCell (position);
				continueFrom.end = navmesh.WorldToCell (position);
				continueFrom.action = AgentAction.LAND;
			}

			if (   continueFrom.action != AgentAction.DROP
				&& continueFrom.action != AgentAction.FLOAT_LEFT
				&& continueFrom.action != AgentAction.FLOAT_RIGHT
				&& continueFrom.action != AgentAction.DASH_LEFT
				&& continueFrom.action != AgentAction.DASH_RIGHT
			) { // Reset the drop count
				dropActionCount = 0;
			}
			if (   continueFrom.action != AgentAction.JUMP_CONTINUE 
				&& continueFrom.action != AgentAction.JUMP 
				&& continueFrom.action != AgentAction.FLOAT_LEFT
				&& continueFrom.action != AgentAction.FLOAT_RIGHT
				&& continueFrom.action != AgentAction.DASH_LEFT
				&& continueFrom.action != AgentAction.DASH_RIGHT
			) { // Reset the jump count
				jumpActionCount = 0;
			}

			// Build the list of next actions if it needs it
			if (continueFrom.nextActions == null) {
				continueFrom.nextActions = new List<CellAction> ();
				List<AgentAction> nextAgentActions = followUpActions [continueFrom.action];
				for (int i = 0; i < nextAgentActions.Count; i++) {
					PlatformerNavMesh.CellPosition pos = GetActionEnd (continueFrom, nextAgentActions [i], dropActionCount, jumpActionCount);
					if (pos != null && pos.x >= 0 && pos.x < navmesh.gridWidth && pos.y >= 0 && pos.y < navmesh.gridHeight) {
						CellAction action = new CellAction ();
						action.action = nextAgentActions [i];
						action.start = continueFrom.end;
						action.end = pos;
						continueFrom.nextActions.Add (action);
					}
				}
				// Sort the next actions list based on which gets us closest to the target
				continueFrom.nextActions = continueFrom.nextActions.OrderBy (item => {
					float dist = Vector2.Distance (target.position, navmesh.CellToWorld(item.end.x, item.end.y));
					return dist;
				}).ThenByDescending( item => {
					return target.position.y - navmesh.CellToWorld(item.end.x, item.end.y).y;
				}).ToList ();
			}
			/*for (int i = 0; i < continueFrom.nextActions.Count; i++) {
				CellAction item = continueFrom.nextActions [i];
				float dist = Vector2.Distance (navmesh.CellToWorld(item.end.x, item.end.y), target.position);
				Debug.LogFormat ("Action: {0}, Distance: {1}", item.action, dist);
			}*/

			bool foundPath = false;
			while (continueFrom.nextActionIndex < continueFrom.nextActions.Count) {
				CellAction action = continueFrom.nextActions [continueFrom.nextActionIndex];
				continueFrom.nextActionIndex++;

				if (navmesh.GetCell (action.end.x, action.end.y)) {
					path.AddLast (action);
					if (action.action == AgentAction.DROP) {
						dropActionCount++;
					} else if (action.action == AgentAction.JUMP || action.action == AgentAction.JUMP_CONTINUE) {
						jumpActionCount++;
					}
					//Debug.LogFormat ("Continuing with action: {0}. Dist: {1}", action.action, Vector2.Distance (target.position, new Vector2 (action.end.x, action.end.y)));
					foundPath = true;
					break;
				}

				if (Time.realtimeSinceStartup - lastYieldTime > maxPerTickPathfindingTime) {
					//Debug.Log ("Yielding to main tick");
					lastYieldTime = Time.realtimeSinceStartup;
					yield return null; // Give the main tick a chance to run
				}
			}

			if (!foundPath) {
				Debug.DrawLine (navmesh.CellToWorld(continueFrom.start.x, continueFrom.start.y), navmesh.CellToWorld(continueFrom.end.x, continueFrom.end.y), Color.cyan, 1.0f);
				Debug.LogFormat ("Reverting with action: {0}. Dist: {1}", continueFrom.action, Vector2.Distance (target.position, new Vector2 (continueFrom.end.x, continueFrom.end.y)));
				path.RemoveLast ();
			}else{
				Debug.DrawLine (navmesh.CellToWorld(continueFrom.end.x, continueFrom.end.y), navmesh.CellToWorld(path.Last.Value.end.x, path.Last.Value.end.y), Color.red, 1.0f);
			}

			if (Time.realtimeSinceStartup - lastYieldTime > maxPerTickPathfindingTime) {
				//Debug.Log ("Yielding to main tick");
				lastYieldTime = Time.realtimeSinceStartup;
				yield return null; // Give the main tick a chance to run
			}
		}
	}

	public PlatformerNavMesh.CellPosition GetActionEnd( CellAction last, AgentAction check, int dropCount, int jumpCount ){
		switch (check) {
		case AgentAction.FLOAT_LEFT:
			return new PlatformerNavMesh.CellPosition (last.end.x - ToCellDistance (floatDistance), last.end.y);

		case AgentAction.MOVE_LEFT:
			//Debug.Log ("Try move Left");
			int landHeight = last.end.y + maxClimbCount;
			bool hitGround = false;
			while (last.end.y - landHeight < maxClimbCount * 2) { // Drop as long as we haven't dropped further than the max drop height
				Debug.DrawLine(navmesh.CellToWorld(last.end.x - 1, landHeight), navmesh.CellToWorld(last.end.x - 1, landHeight - 1), Color.black, 0.5f);
				if (!navmesh.GetCell (last.end.x - 1, landHeight - 1)) { // False is impassible
					hitGround = true;
					break;
				}
				landHeight--;
			}
			if (hitGround) {
				return new PlatformerNavMesh.CellPosition (last.end.x - 1, landHeight);
			} else {
				return null;
			}

		case AgentAction.FLOAT_RIGHT:
			return new PlatformerNavMesh.CellPosition (last.end.x + ToCellDistance (floatDistance), last.end.y);

		case AgentAction.MOVE_RIGHT:
			//Debug.Log ("Try move Right");
			landHeight = last.end.y + maxClimbCount;
			hitGround = false;
			while (last.end.y - landHeight < maxClimbCount * 2) { // Drop as long as we haven't dropped further than the max drop height
				Debug.DrawLine(navmesh.CellToWorld(last.end.x + 1, landHeight), navmesh.CellToWorld(last.end.x + 1, landHeight - 1), Color.black, 0.5f);
				if (!navmesh.GetCell (last.end.x + 1, landHeight - 1)) { // False is impassible
					hitGround = true;
					break;
				}
				landHeight--;
			}
			if (hitGround) {
				return new PlatformerNavMesh.CellPosition (last.end.x + 1, landHeight);
			} else {
				return null;
			}

		case AgentAction.DROP_RIGHT:
			//Check that the player can move left, and that there isn't a cell under that
			if (!navmesh.GetCell (last.end.x + 1, last.end.y) || !navmesh.GetCell (last.end.x + 1, last.end.y - 1)) {
				return null;
			}
			return new PlatformerNavMesh.CellPosition (last.end.x + 1, last.end.y);

		case AgentAction.DROP_LEFT:
			//Check that the player can move left, and that there isn't a cell under that
			if (!navmesh.GetCell (last.end.x - 1, last.end.y) || !navmesh.GetCell (last.end.x - 1, last.end.y - 1)) {
				return null;
			}
			return new PlatformerNavMesh.CellPosition (last.end.x - 1, last.end.y);

		case AgentAction.DASH_LEFT:
			//return new PlatformerNavMesh.CellPosition(last.end.x - ToCellDistance(dashDistance), last.end.y);
			return null;

		case AgentAction.DASH_RIGHT:
			//return new PlatformerNavMesh.CellPosition(last.end.x + ToCellDistance(dashDistance), last.end.y);
			return null;

		case AgentAction.JUMP_CONTINUE:
		case AgentAction.JUMP:
			if (jumpCount > jumpHeight) { // Can't jump higher than this
				return null;
			}
			return new PlatformerNavMesh.CellPosition(last.end.x, last.end.y + 1);

		case AgentAction.DROP:
			// TODO: There are no floors which can be dropped through, but when there are, handle that case
			if (dropCount > maxDropCount || !navmesh.GetCell(last.end.x, last.end.y - 2) ) { // Can't drop further than this, or there is floor below where we'll drop to
				return null;
			}
			return new PlatformerNavMesh.CellPosition(last.end.x, last.end.y - 1);

		case AgentAction.LAND:
			//Debug.Log ("Try To land");
			if ( navmesh.GetCell(last.end.x, last.end.y - 2) ) { // there is no floor below where we'll land
				return null;
			}
			return new PlatformerNavMesh.CellPosition(last.end.x, last.end.y - 1);

		default:
			return null;
		}
	}

	public int ToCellDistance( float unitDistance ){
		return Mathf.RoundToInt(unitDistance / navmesh.gridSize);
	}
}

