using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSwitch : MonoBehaviour {

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
 
	public void LoadScene (string sceneName) {
		Application.LoadLevel (sceneName);
	}
	
	public void ExitGame () {
		Application.Quit(); 
	}
}
