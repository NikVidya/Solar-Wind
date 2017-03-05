using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomSwitch : GenericCollider {
	public string roomName;
    private bool hitOnce = false;

    public override void OnHit() {
        if (hitOnce == false) {
            SceneManager.LoadScene(roomName);
            hitOnce = true;
        }
    }
}
