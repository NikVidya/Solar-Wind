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

    public bool isRespawning = false;

    public bool cantMove = false;

    private float speed;
    private float step;
    public Transform respawnTarget;

	private Animator animator;

	protected override void OnStart(){
		animator = GetComponentInChildren<Animator> ();
		Debug.Assert (animator != null);
	}

    void Update() {
        if (cantMove) {
            animator.SetFloat("speed", 0);
        }
        if (!isRespawning && !cantMove) {
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

			if (Mathf.Abs(input.x) > 0.01) {
				animator.SetInteger ("direction", (int)Mathf.Sign (input.x));
				animator.SetFloat ("speed", 1);
			} else {
				animator.SetFloat ("speed", 0);
			}

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
                    velocity.x = Mathf.SmoothDamp(velocity.x, input.x * moveSpeed , ref midairVelocitySmoothing, inertia );
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
    public void ResetMovement(string playerFacingDirection) {
        velocity.x = 0;
        velocity.y = 0;
        dashTimer = 0;
        hasDashedThisJump = false;
        if (playerFacingDirection == "right") {
            playerDirection = 1;
        } else if (playerFacingDirection == "left") {
            playerDirection = -1;
        }
    }
    public void Death(Transform target) {
        if (!isRespawning) {
            speed = 7;
            step = speed * Time.deltaTime;
            respawnTarget = target;
            isRespawning = true;
            StartCoroutine(Respawn(target));
        }
    }
    IEnumerator Respawn(Transform target) {
        // pauses on player death, then respawns
        yield return new WaitForSeconds(2f);
        while (Mathf.Abs(transform.position.x - target.position.x) > 0.2 || Mathf.Abs(transform.position.y - target.position.y) > 0.2) {
            transform.position = Vector2.MoveTowards(transform.position, respawnTarget.position, step);
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        isRespawning = false;
    }
}
