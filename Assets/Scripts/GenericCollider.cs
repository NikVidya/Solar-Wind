using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * Make a child class of this if you want to have something do something when it collides
 * check RaycastController script if you want specifics for how this works
 * add stuff to OnHit() to make it do stuff...on...hit...
 */
public class GenericCollider : RaycastController {

    public override void Start() {
        base.Start();
    }

    void FixedUpdate() {
        UpdateRaycastOrigins();
        HorizontalCollisions();
        VerticalCollisions();
    }

    public virtual void OnHit() {
        // child classes do what they want with this
    }

    void HorizontalCollisions() {
        float rayLength = 0.03f + skinWidth;

        // draw rays on left side
        for (int i = 0; i < horizontalRayCount; i++) {
            // declares and reverses the ray origin based on move direction
            Vector3 rayOrigin = raycastOrigins.bottomLeft;

            // ray is drawn by horizontal spacing
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);

            // ray is cast from ray origin, in direction * directionX (+ or -), for length rayLength, looking for the collisionmask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.left, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector3.left * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {
                OnHit();
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
                OnHit();
            }
        }
    }

    // Detects collisions above and below the entity
    void VerticalCollisions() {
        float rayLength = 0.03f + skinWidth;

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
                OnHit();
            }
        }
        // draw rays on bottom side
        for (int i = 0; i < verticalRayCount; i++) {
            Vector3 rayOrigin = raycastOrigins.bottomLeft;
            // ray is drawn by vertical spacing
            rayOrigin += Vector3.right * (verticalRaySpacing * i);
            // ray is cast from ray origin, in direction * directionY (+ or -), for length rayLength, stops at collisionMask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {
                OnHit();
            }
        }
    }
    
}
