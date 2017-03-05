using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectRaycasts : RaycastController {

    public LayerMask other;

    public override void Start() {
        base.Start();
    }

    void Update() {
        UpdateRaycastOrigins();
        HorizontalCollisions();
        VerticalCollisions();
    }
    void HorizontalCollisions() {
        float rayLength = 10 + skinWidth;

        // draw rays on left side
        for (int i = 0; i < horizontalRayCount; i++) {
            // declares and reverses the ray origin based on move direction
            Vector3 rayOrigin = raycastOrigins.bottomLeft;

            // ray is drawn by horizontal spacing
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);

            // ray is cast from ray origin, in direction * directionX (+ or -), for length rayLength, looking for the collisionmask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector3.right * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {
            }
        }
        // draw rays on right side
        for (int i = 0; i < horizontalRayCount; i++) {
            // declares and reverses the ray origin based on move direction
            Vector3 rayOrigin = raycastOrigins.bottomRight;

            // ray is drawn by horizontal spacing
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);

            // ray is cast from ray origin, in direction * directionX (+ or -), for length rayLength, looking for the collisionmask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector3.right * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {
            }
        }
    }

    // Detects collisions above and below the entity
    void VerticalCollisions() {
        float rayLength = 10 + skinWidth;

        // draw rays on top side
        for (int i = 0; i < verticalRayCount; i++) {
            Vector3 rayOrigin = raycastOrigins.topLeft;
            // ray is drawn by vertical spacing
            rayOrigin += Vector3.right * (verticalRaySpacing * i);
            // ray is cast from ray origin, in direction * directionY (+ or -), for length rayLength, stops at collisionMask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector3.up * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {

            }
        }
    }
}
