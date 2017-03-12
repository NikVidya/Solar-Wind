using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BridgeRaiseScript : MonoBehaviour {
    public Transform RaisingBridge;
    public Transform ExtendingBridge;
    public float speed = 0.5f;
    bool activated = false;
    float step;
	void Start () {
        step = speed * Time.deltaTime;
	}
    public void Activate() {
        activated = true;
    }
    public void removeScene(GameObject sceneTriggerToRemove) {
        Destroy(sceneTriggerToRemove);
    }
	void Update () {
        if (activated) {
            RaisingBridge.position = Vector3.MoveTowards(RaisingBridge.position, new Vector3(25, 13), step);
            ExtendingBridge.position = Vector3.MoveTowards(ExtendingBridge.position, new Vector3(21, 13), step);
        }
    }
}
