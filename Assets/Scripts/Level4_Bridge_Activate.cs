using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level4_Bridge_Activate : MonoBehaviour {
	public GameObject[] objectsToMove;
	public Transform movingRoof;
	public Transform target;
	public Collider2D exitDoor;
	public void Activate() {
		for (int i = 0; i < objectsToMove.Length; i++) {
			Destroy(objectsToMove[i]);
		}
		movingRoof.position = Vector3.MoveTowards(movingRoof.position, target.position, 5f);
		exitDoor.isTrigger = true;
	}
}
