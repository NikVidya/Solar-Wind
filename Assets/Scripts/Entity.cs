using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller))]
public abstract class Entity : MonoBehaviour {
    float gravity = Constants.Physics.GRAVITY;
    float moveSpeed = Constants.Physics.ENTITY_RUN_SPEED;
    Vector3 velocity;
    protected Controller controller;

    void Start() {
        controller = GetComponent<Controller>();
    }
    void Update() {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        velocity.x = input.x * moveSpeed;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
