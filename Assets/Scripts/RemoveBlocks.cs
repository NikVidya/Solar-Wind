using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveBlocks : MonoBehaviour {

	public GameObject objectToDelete;


	public void Activate() {
		Destroy(objectToDelete);
	}
}
