using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSpike : MonoBehaviour {

	void OnDrawGizmos(){
		Gizmos.DrawIcon (transform.position, "spike.png", true);
	}
}
