using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterLocation : MonoBehaviour {

	public GameObject TeleporterToDelete;

	public void Activate() {
		Destroy(TeleporterToDelete);
	}
}
