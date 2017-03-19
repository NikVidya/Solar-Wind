using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpPoint : MonoBehaviour {

	public Transform warpTarget;

	void OnTriggerEnter2D(Collider2D col){
		col.gameObject.transform.position = warpTarget.position;
	}
		
}
