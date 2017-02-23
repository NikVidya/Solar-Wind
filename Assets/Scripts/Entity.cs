using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller))]
public abstract class Entity : MonoBehaviour {
    // total height of the entity's jump
    public float jumpHeight = 4;
    // time it takes to reach apex of jump
    public float jumpTimeApex = .4f;
    // speed of horizontal running
    public float moveSpeed = 6;
    // player can't control jumps easily
    public float midairAccel = 0.2f;
    protected float midairVelocitySmoothing;

    protected float gravity;
    protected float jumpVelocity;
    protected Vector3 velocity;
    protected Controller controller;

    void Start() {
        controller = GetComponent<Controller>();

        // derived from deltaV = Vinitial * t + (acceleration * time^2)/2
        gravity = -(2 * jumpHeight) / Mathf.Pow(jumpTimeApex, 2);
        // derived from Vfinal = Vinitial + accel * time
        jumpVelocity = Mathf.Abs(gravity) * jumpTimeApex;
    }

    // void Update(){} movement properties are defined in child classes
}
