using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class PlatformerNavMesh : MonoBehaviour {

	public float gridSize = 0.5f; // The unit size of each grid in the nav mesh


	[System.Serializable]
	public class MeshGridCell {
		public int x, y;
		public bool isBlocked;
		public MeshGridCell(int x, int y){
			this.x = x;
			this.y = y;
		}
	}
	[SerializeField]
	MeshGridCell[,] navMesh;

	// --~~== EDITOR SCRIPTS ==~~--

	#if UNITY_EDITOR

	public bool isGeneratingNavMesh = false;
	public Vector2 bbMin, bbMax;

	int layerMask = 1 << 8; // Get anything on the layer 8

	float buildStepTime = 0.01f; // Number of seconds between each step
	float buildTimeout = 10.0f; // Maximum number of seconds to spend building the nav mesh

	public float buildStartTime; // When the build started
	float buildLastStepTime; // Time of the last step

	int curX, curY; // The cells currently being checked

	int gridWidth, gridHeight;

	// Force the frame update function to run every frame, even in the editor
	void OnEnable(){
		EditorApplication.update += FrameUpdate;
	}
	void OnDisable(){
		EditorApplication.update -= FrameUpdate;
	}

	float SnapToGrid(float val){
		return (int)Mathf.Round ((val / gridSize)) * gridSize;
	}

	void OnDrawGizmos(){
		bbMax.x = SnapToGrid (bbMax.x);
		bbMax.y = SnapToGrid (bbMax.y);
		bbMin.x = SnapToGrid (bbMin.x);
		bbMin.y = SnapToGrid (bbMin.y);

		/*Gizmos.color = new Color (0, 255, 255, 0.1f);
		Gizmos.DrawCube ((bbMin + bbMax) / 2.0f, bbMax - bbMin);*/

		if (navMesh != null) {
			for (int x = 0; x < navMesh.GetLength (0); x++) {
				for (int y = 0; y < navMesh.GetLength (1); y++) {
					if (navMesh [x, y] != null) {
						if (navMesh [x, y].isBlocked) {
							Gizmos.color = Color.red;
						} else {
							Gizmos.color = Color.green;
						}
					} else {
						Gizmos.color = Color.yellow;
					}
					Gizmos.DrawWireCube (CellToWorld(new Vector2(x, y)), new Vector2 (gridSize, gridSize));
				}
			}
		}
	}

	public void StartBuilding(){
		isGeneratingNavMesh = true;
		buildStartTime = Time.realtimeSinceStartup;

		// Dump the old navmesh and create a new one
		gridWidth = Mathf.RoundToInt( Mathf.Abs( bbMax.x - bbMin.x ) / gridSize );
		gridHeight = Mathf.RoundToInt( Mathf.Abs( bbMax.y - bbMin.y ) / gridSize );

		navMesh = new MeshGridCell[gridWidth, gridHeight];
		Debug.Log (navMesh);
	}

	void FrameUpdate()
	{
		float time = Time.realtimeSinceStartup;
		if (isGeneratingNavMesh && time - buildLastStepTime > buildStepTime ) {
			buildLastStepTime = time;
			/*if (time - buildStartTime > buildTimeout) {
				Debug.LogWarningFormat ("Navmesh building timed out after {0}s!", time - buildStartTime);
				isGeneratingNavMesh = false;
				return;
			}*/
			if (navMesh == null) {
				isGeneratingNavMesh = false;
				return;
			}

			while (Time.realtimeSinceStartup - buildLastStepTime < buildStepTime) {

				// Check the current box
				MeshGridCell cell = new MeshGridCell (curX, curY);

				Collider2D[] hits = Physics2D.OverlapBoxAll (CellToWorld (new Vector2 (curX, curY)), new Vector2 (gridSize, gridSize), 0, layerMask);
				if (hits.Length > 0) {
					cell.isBlocked = true;
				} else {
					cell.isBlocked = false;
				}

				navMesh [curX, curY] = cell;

				curX++;
				if (curX >= gridWidth) {
					curX = 0;
					curY++;
				}
				if (curY >= gridHeight) {
					curY = 0;
					curX = 0;
					isGeneratingNavMesh = false;
					Debug.Log ("Finished Building Navmesh");
				}
			}

			// Force an update
			SceneView.RepaintAll ();
		}
	}

	#endif

	// --~~== END EDITOR SCRIPTS ==~~--

	public Vector2 CellToWorld(Vector2 cell){
		return ((cell*gridSize) + bbMin + new Vector2(gridSize/2, gridSize/2));
	}
	public Vector2 WorldToCell(Vector2 world){
		Vector2 tmp = (world - bbMin - new Vector2(gridSize/2, gridSize/2)) / gridSize;
		return new Vector2 (Mathf.RoundToInt (tmp.x), Mathf.RoundToInt (tmp.y));
	}
}
