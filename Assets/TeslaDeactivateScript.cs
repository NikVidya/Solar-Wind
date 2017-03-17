using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeslaDeactivateScript : MonoBehaviour {

    public Collider2D[] collidersToDeactivate;
    public Transform toMove;
    public Transform target;
    public GameObject toDelete;
	public void DeactivateTeslas() {
        for (int i = 0; i < collidersToDeactivate.Length; i++) {
            collidersToDeactivate[i].isTrigger = false;
        }
        toMove.position = target.position;
        Destroy(toDelete);
}
}
