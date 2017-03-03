using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity : Entity {
    public float dashSpeed = 10;
    private bool isDashing = false;
    private bool hasDashedThisJump = false;
    private float playerDirection = 1;
    void Update() {
        // stops gravity from changing when colliding vertically
        if (controller.collisions.above || controller.collisions.below) {
            velocity.y = 0;
            hasDashedThisJump = false;
        }
        // wasd or ^v<> key input for movement
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
            // player's direction is changeable midair
            if (input.x != 0) {
                playerDirection = input.x;
            }
            if (Input.GetKeyDown(KeyCode.Space) && !hasDashedThisJump) {
                velocity.x = dashSpeed * playerDirection; // make this depend on the player's facing direction once we have sprites
                hasDashedThisJump = true;
            }

        } else {
            velocity.x = input.x * moveSpeed;
            if (input.x != 0) {
                playerDirection = input.x;
            }
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
