using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomSwitch : GenericCollider {
	public string roomName;

    new void Start() {
        
    }
    new void OnHit() {
        SceneManager.LoadScene(roomName);
    }

}
