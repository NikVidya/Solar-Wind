using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorUnlockScript : MonoBehaviour {

    public GameObject doorToDelete;
    public GameObject characterToDelete;

    public void Activate() {
        Destroy(doorToDelete);
        Destroy(characterToDelete);
    }
}
