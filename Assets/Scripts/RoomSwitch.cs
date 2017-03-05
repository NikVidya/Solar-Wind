using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSwitch : MonoBehaviour {

	public string roomName;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	void OnTrigger2D(Collision2D other){
		Debug.Log("hit");
		Application.LoadLevel (roomName);
		if (other.gameObject.name == "player"){
			Application.LoadLevel (roomName);
		}
		if (other.gameObject.tag == "Player"){
			Application.LoadLevel (roomName);
		}
	}
}
