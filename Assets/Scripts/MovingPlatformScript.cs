using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformScript : RaycastController {
    
    public LayerMask passengerMask;
	float xOrigin, yOrigin;

    public Vector3[] localWaypoints;

    Vector3[] globalWaypoints;

    public float speed;
    int fromWaypointIndex;
    float percentBetweenWaypoints;

    public override void Start() {
        base.Start();
        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++) {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }
    

    void Update() {
        UpdateRaycastOrigins();

        Vector3 velocity = CalculatePlatformMovement();
        MovePassengers(velocity);
        transform.Translate(velocity);
    }

    Vector3 CalculatePlatformMovement() {
        int toWaypointIndex = fromWaypointIndex + 1;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;

        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], percentBetweenWaypoints);

        if (percentBetweenWaypoints >= 1) {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;
            if (fromWaypointIndex >= globalWaypoints.Length - 1) {
                fromWaypointIndex = 0;
                System.Array.Reverse(globalWaypoints);
            }
        }

        return newPos - transform.position;
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

    void OnDrawGizmos() {
        if (localWaypoints != null) {
            Gizmos.color = Color.red;
            float size = .3f;
            
            for(int i = 0; i < localWaypoints.Length; i++) {
                Vector3 globalWaypointPosition = (Application.isPlaying)?globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPosition - Vector3.up * size, globalWaypointPosition + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPosition - Vector3.left * size, globalWaypointPosition + Vector3.left * size);
            }
        }
    }
				
}
