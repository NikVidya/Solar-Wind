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
	public float fallAccel = 9.81f;
	public float groundMoveSpeed = 5.0f;
	public float groundAccel = 0.5f;

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
	public CapsuleCollider2D myCollider;

	int layerMask = 1 << 8;

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
		LAND,
		ARRIVE
	}
	// LUT for which actions can follow other actions (i.e. jump can't follow jump)
	public Dictionary<AgentAction, List<AgentAction>> followUpActions = new Dictionary<AgentAction, List<AgentAction>>
	{
		{ AgentAction.MOVE_LEFT, 		new List<AgentAction>(){AgentAction.MOVE_LEFT,	AgentAction.MOVE_RIGHT,		AgentAction.JUMP, 			AgentAction.DROP,		AgentAction.DROP_LEFT,		AgentAction.ARRIVE } },
		{ AgentAction.MOVE_RIGHT, 		new List<AgentAction>(){AgentAction.MOVE_RIGHT,	AgentAction.MOVE_LEFT,		AgentAction.JUMP, 			AgentAction.DROP,		AgentAction.DROP_RIGHT,		AgentAction.ARRIVE } },
		{ AgentAction.DROP_RIGHT, 		new List<AgentAction>(){AgentAction.DROP,		AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_RIGHT,	AgentAction.LAND,			AgentAction.ARRIVE } },
		{ AgentAction.DROP_LEFT, 		new List<AgentAction>(){AgentAction.DROP,		AgentAction.FLOAT_LEFT, 	AgentAction.DASH_LEFT,	AgentAction.LAND,			AgentAction.ARRIVE } },
		{ AgentAction.FLOAT_LEFT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND, 			AgentAction.FLOAT_LEFT,	AgentAction.FLOAT_RIGHT,	AgentAction.ARRIVE } },
		{ AgentAction.FLOAT_RIGHT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND, 			AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT,	AgentAction.ARRIVE } },
		{ AgentAction.JUMP, 			new List<AgentAction>(){AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_LEFT, 	AgentAction.DASH_RIGHT, 	AgentAction.LAND,		AgentAction.JUMP_CONTINUE,	AgentAction.ARRIVE} },
		{ AgentAction.JUMP_CONTINUE,	new List<AgentAction>(){AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_LEFT, 	AgentAction.DASH_RIGHT, 	AgentAction.LAND, 		AgentAction.JUMP_CONTINUE,	AgentAction.ARRIVE} },
		{ AgentAction.DROP, 			new List<AgentAction>(){AgentAction.FLOAT_LEFT, AgentAction.FLOAT_RIGHT, 	AgentAction.DASH_LEFT, 	AgentAction.DASH_RIGHT, 	AgentAction.DROP, 		AgentAction.LAND,			AgentAction.ARRIVE} },
		{ AgentAction.LAND, 			new List<AgentAction>(){AgentAction.MOVE_LEFT, 	AgentAction.MOVE_RIGHT, 	AgentAction.DROP, 		AgentAction.JUMP,			AgentAction.ARRIVE } },
		{ AgentAction.DASH_LEFT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND,			AgentAction.ARRIVE} 	},
		{ AgentAction.DASH_RIGHT, 		new List<AgentAction>(){AgentAction.DROP, 		AgentAction.LAND,			AgentAction.ARRIVE} 	},
		{ AgentAction.ARRIVE, 			new List<AgentAction>(){AgentAction.MOVE_LEFT,	AgentAction.MOVE_RIGHT,		AgentAction.JUMP, 		AgentAction.DROP_LEFT,		AgentAction.DROP_RIGHT,	AgentAction.DROP}}
	};
	public class CellAction {
		public PlatformerNavMesh.CellPosition start, end;
		public AgentAction action;
		public List<CellAction> nextActions;
		public int nextActionIndex;
	}
	LinkedList<CellAction> path = new LinkedList<CellAction> (); // The path for the NPC to take

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

	void OnDrawGizmos(){
		Gizmos.color = Color.yellow;
		for (LinkedListNode<CellAction> i = path.First; i != null; i = i.Next) {
			Gizmos.DrawWireSphere (navmesh.CellToWorld (i.Value.end.x, i.Value.end.y), 0.1f);
		}
		if (currentAction != null) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere (currentAction.endPos, 0.2f);
		}
	}


	IEnumerator pathProducer;
	void Start(){
		lastTeleport = Time.time;
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



	Vector2 vel = new Vector2 (0, 0);

	public void Move(float dir){
		vel.x = Mathf.Clamp (dir, -1, 1) * groundMoveSpeed;//Mathf.Clamp (vel.x + (groundAccel * Mathf.Clamp(dir,-1,1) * Time.deltaTime), -groundMoveSpeed, groundMoveSpeed);
		// Check if the agent should fall
		Vector2 castOrigin = (Vector2)myCollider.bounds.center + new Vector2(0, myCollider.size.y);
		RaycastHit2D hit = Physics2D.CapsuleCast(castOrigin, myCollider.size, myCollider.direction, 0, Vector2.down, myCollider.size.y, layerMask);
		if (hit.collider != null) {
			// Place on surface
			//Debug.Log ("Place on Surface");
			vel.y = 0;
			transform.position = new Vector2 (transform.position.x, castOrigin.y - hit.distance);
		} else {
			//Debug.Log ("Is falling");
			vel.y = Mathf.Clamp( vel.y - (fallAccel * 10 * Time.deltaTime), -10, 10);
		}
		transform.position += (Vector3)vel * Time.deltaTime;
	}

	float startConsumingTime;
	bool isConsuming = false;
	bool shouldGround = true;

	public class ConsumerAction
	{
		public Vector2 startPos;
		public Vector2 endPos;
		public virtual void Update(PathingAgent agent){
			Debug.LogWarning ("Performing default action update");
			agent.transform.position = endPos;
		}
		public virtual bool IsComplete(){
			return true;
		}
	}
	public class MoveAction : ConsumerAction
	{
		float curX;
		public override void Update(PathingAgent agent){
			Debug.LogFormat ("Doing move action {0}", endPos.x - startPos.x);
			agent.Move (Mathf.Sign(endPos.x - startPos.x));
			curX = agent.transform.position.x;
		}
		public override bool IsComplete(){
			if (Mathf.Abs (curX - endPos.x) <= 1.0f) {
				Debug.Log ("Finished MoveAction");
				return true;
			} else {
				return false;
			}
		}
	}
	public class JumpAction : ConsumerAction
	{
		public float apex;
		public float t = 0;

		Vector2 v1, v2, apexPos;
		public void Init(){
			apexPos = new Vector2 (startPos.x + ((endPos.x - startPos.x) / 2.0f), apex);
			v1 = 2 * startPos - 4 * apexPos + 2 * endPos;
			v2 = -2 * startPos + 2 * endPos;
		}

		public override void Update(PathingAgent agent){
			// Set velocity
			if (Mathf.Abs (agent.vel.x) < agent.groundMoveSpeed) {
				agent.vel.x = Mathf.Sign (agent.vel.x) * agent.groundMoveSpeed;
			}
			// Correct the velocity of the agent to match the jump
			if ( (startPos.x < endPos.x && agent.vel.x < 0) || (startPos.x > endPos.x && agent.vel.x > 0) ) {
				agent.vel.x = -agent.vel.x;
			}
			Debug.DrawLine (startPos, apexPos, Color.white, 1);
			Debug.DrawLine (apexPos, endPos, Color.magenta, 1);

			Vector2 cp = Mathf.Pow (1 - t, 2) * startPos + 2 * (1 - t) * t * apexPos + Mathf.Pow (t, 2) * endPos;

			t += /*Mathf.Abs(agent.vel.x * Time.deltaTime)*/3 * Time.deltaTime / (t * v1 + v2).magnitude;

			agent.transform.position = cp;
		}
		public override bool IsComplete(){
			return t >= 1;
		}
	}

	ConsumerAction currentAction;
	float lastTeleport;
	void Update(){
		if (Time.time - lastTeleport > secondsBeforeTeleport) {
			path.Clear ();
			transform.position = target.position;
			currentAction = null;
			lastTeleport = Time.time;
		}

		if (currentAction == null && path.First != null) {
			//Debug.Log ("Getting new action");
			// Build the next action from the path
			CellAction action = path.First.Value;
			CellAction check;

			switch (action.action) {
			case AgentAction.DROP:
			case AgentAction.DROP_LEFT:
			case AgentAction.DROP_RIGHT:
			case AgentAction.JUMP:
				currentAction = new JumpAction ();
				currentAction.startPos = navmesh.CellToWorld (action.end.x, action.end.y);
				float apexHeight = currentAction.startPos.y;
				do { // Count everything from here till the next time we land as a jump
					check = path.First.Value;
					Vector2 world = navmesh.CellToWorld (check.end.x, check.end.y);
					if (world.y > apexHeight) {
						apexHeight = world.y;
					}
					path.RemoveFirst ();
				} while(path.First != null && check.action != AgentAction.LAND);
				((JumpAction)currentAction).apex = apexHeight;
				currentAction.endPos = navmesh.CellToWorld (check.end.x, check.end.y);
				((JumpAction)currentAction).Init ();
				break;

			case AgentAction.MOVE_LEFT:
			case AgentAction.MOVE_RIGHT:
				currentAction = new MoveAction ();
				currentAction.startPos = navmesh.CellToWorld (action.end.x, action.end.y);
				// Find when we stop moving or change direction
				do { // The only thing that should be almagamated into the move action is move_left/right, and only when it's the same direction
					check = path.First.Value;
					path.RemoveFirst ();
				} while (path.First != null && (check.action == AgentAction.MOVE_LEFT || check.action == AgentAction.MOVE_RIGHT || check.action == AgentAction.LAND));
				currentAction.endPos = navmesh.CellToWorld (check.end.x, check.end.y);
				break;
				
			default:
				currentAction = new ConsumerAction ();
				currentAction.startPos = navmesh.CellToWorld (action.end.x, action.end.y);
				// Find when we stop doing whatever it is we are doing
				do {
					check = path.First.Value;
					path.RemoveFirst ();
				} while(path.First != null && check.action == action.action);
				currentAction.endPos = navmesh.CellToWorld (check.end.x, check.end.y);
				break;
			}
		}

		// Update position
		if (currentAction == null || currentAction.IsComplete ()) {
			currentAction = null;
			Debug.Log ("No action or action is complete");
			return;
		} else {
			Debug.Log ("Update the current action");
			currentAction.Update (this);
		}
	}

	IEnumerator ProducePath(){
		//Debug.Log ("Starting up producer");
		if (navmesh == null) {
			yield break;
		}

		int dropActionCount = 0; // How many times has the agent done DROP
		int jumpActionCount = 0; // How many times has the agent done JUMP_CONTINUE

		float lastYieldTime = Time.realtimeSinceStartup;
		while (shouldGeneratePath) {
			if (Time.realtimeSinceStartup - lastYieldTime > maxPerTickPathfindingTime) {
				lastYieldTime = Time.realtimeSinceStartup;
				yield return null; // Give the main tick a chance to run
			}

			// See if we can skip this loop and stop generating
			if (path.Last != null) {
				if (Vector2.Distance (navmesh.CellToWorld (path.Last.Value.end.x, path.Last.Value.end.y), target.position) < targetDistanceTollerance) {
					//Debug.Log ("Don't produce more nodes");
					continue;
				}
			}

			// Get the action we are continuing from
			CellAction continueFrom = null;
			if (path.Last != null) {
				continueFrom = path.Last.Value;
			}

			if (continueFrom == null) {
				//Debug.Log ("Making up a node");
				// Make one up. The actor just landed
				continueFrom = new CellAction();
				if (currentAction != null) {
					continueFrom.start = navmesh.WorldToCell (currentAction.startPos);
					continueFrom.end = navmesh.WorldToCell (currentAction.endPos);
				} else {
					continueFrom.start = navmesh.WorldToCell (transform.position);
					continueFrom.end = navmesh.WorldToCell (transform.position);
				}
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
				//Debug.Log ("Building action list");
				continueFrom.nextActions = new List<CellAction> ();
				List<AgentAction> nextAgentActions = followUpActions [continueFrom.action];
				for (int i = 0; i < nextAgentActions.Count; i++) {
					PlatformerNavMesh.CellPosition pos = GetActionEnd (continueFrom, nextAgentActions [i], dropActionCount, jumpActionCount);
					// Check if we've visited this grid already
					if ( pos != null && pos.x >= 0 && pos.x < navmesh.gridWidth && pos.y >= 0 && pos.y < navmesh.gridHeight ) {
						CellAction action = new CellAction ();
						action.action = nextAgentActions [i];
						action.start = continueFrom.end;
						action.end = pos;
						continueFrom.nextActions.Add (action);
					}
				}

				PlatformerNavMesh.CellPosition targetCellPos = navmesh.WorldToCell(target.position);
				continueFrom.nextActions = continueFrom.nextActions.OrderBy (item => {
					if( Mathf.Sign( targetCellPos.x - item.start.x ) != Mathf.Sign( item.end.x - item.start.x ) ){
						return Mathf.Infinity;
					}else if( Mathf.Abs(targetCellPos.x - item.start.x) > 5 ){
						return targetCellPos.y - item.end.y;
					}else{
						return Vector2.Distance(target.position, navmesh.CellToWorld(item.end.x, item.end.y));
					}
				}).ToList ();
			}

			while (continueFrom.nextActions.Count > 0) {
				CellAction action = continueFrom.nextActions [0];
				//Debug.LogFormat ("Attempting Action: {0}", action.action);

				if (navmesh.GetCell (action.end.x, action.end.y)) {
					if (action.action == AgentAction.DROP || action.action == AgentAction.FLOAT_LEFT || action.action == AgentAction.FLOAT_RIGHT) {
						dropActionCount++;
					} else if (action.action == AgentAction.JUMP || action.action == AgentAction.JUMP_CONTINUE) {
						jumpActionCount++;
					}

					// Add the action to the path
					path.AddLast (action);
					Debug.DrawLine (navmesh.CellToWorld(action.start.x, action.start.y), navmesh.CellToWorld(action.end.x, action.end.y), Color.red, 1.0f);

					// This action has been considered, remove it from the node
					continueFrom.nextActions.RemoveAt (0);

					break;
				}

				// This action has been considered, remove it from the node
				continueFrom.nextActions.RemoveAt (0);
			}
			if( continueFrom.nextActions.Count <= 0 ){
				//Debug.DrawLine (navmesh.CellToWorld(continueFrom.start.x, continueFrom.start.y), navmesh.CellToWorld(continueFrom.end.x, continueFrom.end.y), Color.cyan, 1.0f);
				path.RemoveLast ();
			}
		}
	}

	public PlatformerNavMesh.CellPosition GetActionEnd( CellAction last, AgentAction check, int dropCount, int jumpCount ){
		switch (check) {
		case AgentAction.FLOAT_LEFT:
			return new PlatformerNavMesh.CellPosition (last.end.x - 1, last.end.y-1);

		case AgentAction.MOVE_LEFT:
			//Debug.Log ("Try move Left");
			int landHeight = last.end.y + maxClimbCount;
			bool hitGround = false;
			while (last.end.y - landHeight < maxClimbCount * 2) { // Drop as long as we haven't dropped further than the max drop height
				//Debug.DrawLine(navmesh.CellToWorld(last.end.x - 1, landHeight), navmesh.CellToWorld(last.end.x - 1, landHeight - 1), Color.black, 0.5f);
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
			return new PlatformerNavMesh.CellPosition (last.end.x + 1, last.end.y-1);

		case AgentAction.MOVE_RIGHT:
			//Debug.Log ("Try move Right");
			landHeight = last.end.y + maxClimbCount;
			hitGround = false;
			while (last.end.y - landHeight < maxClimbCount * 2) { // Drop as long as we haven't dropped further than the max drop height
				//Debug.DrawLine(navmesh.CellToWorld(last.end.x + 1, landHeight), navmesh.CellToWorld(last.end.x + 1, landHeight - 1), Color.black, 0.5f);
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
			return new PlatformerNavMesh.CellPosition(last.end.x - ToCellDistance(dashDistance), last.end.y);
			//return null;

		case AgentAction.DASH_RIGHT:
			return new PlatformerNavMesh.CellPosition(last.end.x + ToCellDistance(dashDistance), last.end.y);
			//return null;

		case AgentAction.JUMP_CONTINUE:
		case AgentAction.JUMP:
			if (jumpCount > jumpHeight) { // Can't jump higher than this
				return null;
			}
			return new PlatformerNavMesh.CellPosition(last.end.x, last.end.y + 1);

		case AgentAction.DROP:
			// TODO: There are no floors which can be dropped through, but when there are, handle that case
			if (dropCount > maxDropCount /*|| !navmesh.GetCell(last.end.x, last.end.y - 2)*/ ) { // Can't drop further than this, or there is floor below where we'll drop to
				return null;
			}
			return new PlatformerNavMesh.CellPosition(last.end.x, last.end.y - 1);

		case AgentAction.LAND:
			//Debug.Log ("Try To land");
			/*if (navmesh.GetCell (last.end.x, last.end.y - 2)) {
				return null;
			}*/
			return new PlatformerNavMesh.CellPosition(last.end.x, last.end.y - 1);

		case AgentAction.ARRIVE:
			return null;
			/*PlatformerNavMesh.CellPosition targetCellPos = navmesh.WorldToCell (target.position);
			if ( Mathf.Abs(targetCellPos.y - last.end.y) < 4 && Mathf.Abs(targetCellPos.x - last.end.x) < 6 ) {
				return new PlatformerNavMesh.CellPosition (last.end.x, last.end.y);
			} else {
				return null;
			}*/
		default:
			return null;
		}
	}

	public int ToCellDistance( float unitDistance ){
		return Mathf.RoundToInt(unitDistance / navmesh.gridSize);
	}
}

