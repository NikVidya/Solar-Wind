using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformScript : RaycastController {

    public Vector3 move;
    public LayerMask passengerMask;
	float xOrigin, yOrigin;


    public override void Start() {
        base.Start();
		xOrigin = transform.position.x;
		yOrigin = transform.position.y;
    }

    void Update() {
        UpdateRaycastOrigins();

        Vector3 velocity = move * Time.deltaTime;

        MovePassengers(velocity);
        transform.Translate(velocity);
		checkReverse(velocity);
    }

    void MovePassengers(Vector3 velocity) {

        HashSet<Transform> movedPassengers = new HashSet<Transform>();

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // vertically moving platform
        if (velocity.y != 0) {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++) {
                // declares and reverses the ray origin based on move direction
                Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                // ray is drawn by vertical spacing
                rayOrigin += Vector3.right * (verticalRaySpacing * i);
                // ray is cast from ray origin, in direction * directionY (+ or -), for length rayLength, stops at collisionMask
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit) {
                    if (!movedPassengers.Contains(hit.transform)) {
                        movedPassengers.Add(hit.transform);
                        // if the platform is moving down, don't change x
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        // move y of passenger by velocity minus the distance between them
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }

        // horizontally moving platform
        if (velocity.x != 0) {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++) {
                // declares and reverses the ray origin based on move direction
                Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                // ray is drawn by horizontal spacing
                rayOrigin += Vector3.up * (horizontalRaySpacing * i);
                // ray is cast from ray origin, in direction * directionX (+ or -), for length rayLength, looking for the collisionmask
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit) {
                    if (!movedPassengers.Contains(hit.transform)) {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = 0;

                        hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }

        //passenger is on top of horizontally or downward moving platform
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0) {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++) {
                // declares and reverses the ray origin based on move direction
                Vector3 rayOrigin = raycastOrigins.topLeft + Vector3.right * (verticalRaySpacing * i);

                // ray is cast from ray origin, in direction * directionY (+ or -), for length rayLength, stops at collisionMask
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit) {
                    if (!movedPassengers.Contains(hit.transform)) {
                        movedPassengers.Add(hit.transform);

                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }

    }

	void checkReverse(Vector3 velocity){

		if (velocity.y != 0) {
			float distanceLimitY = 4.0f;
			if (transform.position.y > yOrigin + distanceLimitY) {
				move = -move;
			}
			else if (transform.position.y < yOrigin - distanceLimitY) {
				move = -move;
			}
		}

		if (velocity.x != 0) {
			float distanceLimitX = 4.0f;
			if (transform.position.x > xOrigin + distanceLimitX) {
				move = -move;
			}
			else if (transform.position.x < xOrigin - distanceLimitX) {
				move = -move;
			}
		}

	
	}

				
}
