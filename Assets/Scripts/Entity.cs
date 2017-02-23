using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller))]
public abstract class Entity : MonoBehaviour {
    protected Controller controller;
    void Start() {
        controller = GetComponent<Controller>();
    }
    void Update() {

    }
}
