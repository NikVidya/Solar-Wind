using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicNavElement : MonoBehaviour {

	public Vector2 min, max;

	#if UNITY_EDITOR


	void OnDrawGizmos(){
		// Check if the handles need to be reversed
		if (min.x > max.x) {
			float tmp = min.x;
			min.x = max.x;
			max.x = tmp;
		}
		if (min.y > max.y) {
			float tmp = min.y;
			min.y = max.y;
			max.y = tmp;
		}

		Gizmos.color = new Color (255, 255, 0, 0.1f);
		Gizmos.DrawCube ((min + max) / 2.0f, max - min);
	}
	#endif

	// Use this for initialization
	void Start () {
		// Find all nav meshes in the scene
		PlatformerNavMesh[] navMeshes = GameObject.FindObjectsOfType<PlatformerNavMesh>();
		if (navMeshes.Length <= 0) {
			Debug.LogWarning ("Dynamic nav element in scene with no nav mesh");
		} else {
			for (int i = 0; i < navMeshes.Length; i++) {
				PlatformerNavMesh.CellPosition start = navMeshes [i].WorldToCell (min);
				PlatformerNavMesh.CellPosition end = navMeshes [i].WorldToCell (max);
				navMeshes [i].AddDynamicRegion (new PlatformerNavMesh.DynamicNavRegion (start, end));
			}
		}
	}
}
