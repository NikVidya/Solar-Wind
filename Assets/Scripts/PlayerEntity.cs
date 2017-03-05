using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity : Entity {
	public float dashSpeed = 10;
	private bool hasDashedThisJump = false;
	private float dashStartTime = 0.2f;
	private float dashTimer;
	private bool dashCommand = false;
	private float dashCommandTimer;
	private float dashCommandTimerStart = 0.2f;
	private float playerDirection = 1;
	void Update() {
		// stops gravity from changing when colliding vertically
		if (controller.collisions.above || controller.collisions.below || dashTimer >= 0) {
			velocity.y = 0;
		}
		if (controller.collisions.below) {
			hasDashedThisJump = false;
			dashTimer = -1;
		}
		// wasd or ^v<> key input for movement
		Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		// double tap a direction to dash
		if (dashCommandTimer >= 0) {
			dashCommandTimer -= Time.deltaTime;
		} else {
			dashCommand = false;
		}
		if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A)) {
			if (dashCommandTimer >= 0) {
				dashCommand = true;
			} else {
				dashCommandTimer = dashCommandTimerStart;
			}
		}
		if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow)) {
			if (dashCommandTimer >= 0) {
				dashCommand = true;
			} else {
				dashCommandTimer = dashCommandTimerStart;
			}
		}
		// jump with space or W (can't jump unless on ground)
		if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && controller.collisions.below) {
			velocity.y = jumpVelocity;
		}
		// if player is off the ground, direction control isn't as easy
		if (!controller.collisions.below) {
			if (controller.collisions.left || controller.collisions.right) {
				velocity.x = 0;
			}
			if (dashTimer < 0) {
				velocity.x = Mathf.SmoothDamp(velocity.x, input.x * moveSpeed, ref midairVelocitySmoothing, interia);
			}
			// player's direction is changeable midair
			if (input.x != 0 && dashTimer < 0) {
				playerDirection = input.x;
			}
			if (dashCommand && !hasDashedThisJump) {
				velocity.x = dashSpeed * playerDirection; // make this depend on the player's facing direction once we have sprites
				hasDashedThisJump = true;
				dashTimer = dashStartTime;
			}

		} else {
			if (dashTimer < 0) {
				velocity.x = input.x * moveSpeed;
			}
			if (input.x != 0) {
				playerDirection = input.x;
			}
		}
		if (dashTimer < 0) {
			velocity.y += gravity * Time.deltaTime;
		} else {
			dashTimer -= Time.deltaTime;
		}
		controller.Move(velocity * Time.deltaTime);
	}
}