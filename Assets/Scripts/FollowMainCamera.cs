using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMainCamera : MonoBehaviour {
    Camera target;
	void Start () {
            target = Camera.main;
    }
	
	void Update () {
        if (target.isActiveAndEnabled) {
            transform.position = new Vector3(target.transform.position.x + 7.5f, target.transform.position.y + 1.6f, 5);
        }
	}
}
