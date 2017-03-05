using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerSceneTrigger : MonoBehaviour {

    public Scene scene;

    void OnTriggerEnter(Collider other) {
        scene.StartScene();
        Debug.Log("The scene should have started");
    }
}
