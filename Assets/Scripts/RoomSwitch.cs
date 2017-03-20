using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomSwitch : MonoBehaviour {
	public string roomName;

    void OnTriggerEnter(Collider other) {
            SceneManager.LoadScene(roomName);
    }
}
