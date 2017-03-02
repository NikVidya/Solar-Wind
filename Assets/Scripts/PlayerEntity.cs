using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity : Entity {
    void Update() {
        // stops gravity from changing when colliding vertically
        if (controller.collisions.above || controller.collisions.below) {
            velocity.y = 0;
        }
        // wasd or ^v<> key imput for movement
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // jump with space (can't jump unless on ground)
        if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below) {
            velocity.y = jumpVelocity;
        }
        // if player is off the ground, direction control isn't as easy
        if (!controller.collisions.below) {
            if (controller.collisions.left || controller.collisions.right) {
                velocity.x = 0;
            }
            velocity.x = Mathf.SmoothDamp(velocity.x, input.x * moveSpeed, ref midairVelocitySmoothing, midairAccel);
        } else {
            velocity.x = input.x * moveSpeed;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
