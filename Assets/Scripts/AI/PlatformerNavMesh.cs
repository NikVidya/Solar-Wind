using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlatformerNavMesh : MonoBehaviour {

	[Header("Nav Grid Properties")]
	public float gridSize = 0.5f; // The unit size of each grid in the nav mesh
	[Tooltip("Seconds per coroutine tick to dedicate to building the nav mesh")]
	public float buildStepTime = 0.01f;
	[Tooltip("Navable region")]
	public Vector2 bbMin, bbMax;

	int layerMask = 1 << 8; // Get anything on the layer 8

	bool[] navMesh;
	[HideInInspector]
	public int gridWidth, gridHeight;

	bool isUpdatingDynamicRegions = false;

	public class CellPosition {
		public int x, y;
		public CellPosition(int x, int y){
			this.x = x;
			this.y = y;
		}
		public CellPosition(Vector2 pos){
			this.x = Mathf.RoundToInt(pos.x);
			this.y = Mathf.RoundToInt(pos.y);
		}
	}

	public struct DynamicNavRegion {
		public CellPosition start, end;
		public DynamicNavRegion(CellPosition start, CellPosition end){
			this.start = start;
			this.end = end;
		}
	}
	private List<DynamicNavRegion> dynamicRegions = new List<DynamicNavRegion> ();

	// --~~== EDITOR SCRIPTS ==~~--

	#if UNITY_EDITOR

	float SnapToGrid(float val){
		return (int)Mathf.Round ((val / gridSize)) * gridSize;
	}

	void OnDrawGizmos(){
		bbMax.x = SnapToGrid (bbMax.x);
		bbMax.y = SnapToGrid (bbMax.y);
		bbMin.x = SnapToGrid (bbMin.x);
		bbMin.y = SnapToGrid (bbMin.y);

		if (navMesh != null) {
			for (int x = 0; x < gridWidth; x++) {
				for (int y = 0; y < gridHeight; y++) {
					if ( GetCell (x, y) ) {
						Gizmos.color = Color.green;
					} else {
						Gizmos.color = Color.red;
					}
					Gizmos.DrawWireCube (CellToWorld (x, y), new Vector2 (gridSize, gridSize));
				}
			}
		} else {
			Gizmos.color = new Color (0, 255, 255, 0.1f);
			Gizmos.DrawCube ((bbMin + bbMax) / 2.0f, bbMax - bbMin);
		}
	}
	#endif

	// --~~== END EDITOR SCRIPTS ==~~--

	void Awake(){
		gridWidth = Mathf.RoundToInt (Mathf.Abs (bbMax.x - bbMin.x) / gridSize);
		gridHeight = Mathf.RoundToInt (Mathf.Abs (bbMax.y - bbMin.y) / gridSize);

		navMesh = new bool[gridWidth * gridHeight];

		IEnumerator buildingCoroutine = BuildNavRegion (0, 0, gridWidth, gridHeight);
		StartCoroutine (buildingCoroutine);
	}

	IEnumerator BuildNavRegion(int startX, int startY, int endX, int endY){
		float lastYieldTime = Time.realtimeSinceStartup;
		for (int x = startX; x < endX; x++) {
			for (int y = startY; y < endY; y++) {
				Collider2D[] hits = Physics2D.OverlapBoxAll (CellToWorld (x, y), new Vector2 (gridSize, gridSize), 0, layerMask);
				SetCell(x, y, hits.Length <= 0);
				if (Time.realtimeSinceStartup - lastYieldTime > buildStepTime) {
					yield return null;
				}
			}
		}
	}

	IEnumerator dynamicRegionCoroutine;
	void Update(){
		if (!isUpdatingDynamicRegions) {
			isUpdatingDynamicRegions = true;
			if (dynamicRegionCoroutine != null) {
				StopCoroutine (dynamicRegionCoroutine);
			}
			dynamicRegionCoroutine = UpdateDynamicRegions ();
			StartCoroutine (dynamicRegionCoroutine);
		}
	}
	IEnumerator UpdateDynamicRegions(){
		for (int i = 0; i < dynamicRegions.Count; i++) {
			
			float lastYieldTime = Time.realtimeSinceStartup;
			int startX = dynamicRegions [i].start.x;
			int startY = dynamicRegions [i].start.y;
			int endX = dynamicRegions [i].end.x;
			int endY = dynamicRegions [i].end.y;
			for (int x = startX; x < endX; x++) {
				for (int y = startY; y < endY; y++) {
					Collider2D[] hits = Physics2D.OverlapBoxAll (CellToWorld (x, y), new Vector2 (gridSize, gridSize), 0, layerMask);
					SetCell(x, y, hits.Length <= 0);
					if (Time.realtimeSinceStartup - lastYieldTime > buildStepTime) {
						yield return null;
					}
				}
			}

			yield return null;
		}
		isUpdatingDynamicRegions = false;
	}

	public void ClearNav(){
		navMesh = null;
	}

	public void AddDynamicRegion(DynamicNavRegion region){
		dynamicRegions.Add (region);
	}

	private void SetCell(int x, int y, bool blocked){
		if (navMesh != null) {
			navMesh [gridWidth * y + x] = blocked;
		}
	}

	public bool GetCell(int x, int y){
		if (navMesh != null && (gridWidth * y + x) < navMesh.Length && (gridWidth * y + x) > 0) {
			return navMesh [gridWidth * y + x];
		} else {
			return false;
		}
	}

	public Vector2 CellToWorld(int x, int y){
		return new Vector2( bbMin.x + (x * gridSize) + gridSize/2, bbMin.y + (y * gridSize) + gridSize/2);
	}
	public CellPosition WorldToCell(Vector2 world){
		return new CellPosition ((world - bbMin - new Vector2(gridSize/2, gridSize/2)) / gridSize);
	}
}
