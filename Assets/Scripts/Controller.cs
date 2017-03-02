using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class Controller : MonoBehaviour {

    public LayerMask collisionMask;

    static float skinWidth = .15f;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    private float horizontalRaySpacing, verticalRaySpacing;

    private BoxCollider2D collider;
    private RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    public float maxClimbAngle = 80f;
    public float maxDescendAngle = -80f;

	void Start () {
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
    }

    public void Move(Vector3 velocity) {
        UpdateRaycastOrigins();
        collisions.Reset();
        if (velocity.x != 0) {
            HorizontalCollisions(ref velocity);
        }
        if (velocity.y != 0) {
            VerticalCollisions(ref velocity);
        }

        transform.Translate(velocity);
    }
    // Detects collisions left and right of the entity
    void HorizontalCollisions(ref Vector3 velocity) {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++) {
            // declares and reverses the ray origin based on move direction
            Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            // ray is drawn by horizontal spacing
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);
            // ray is cast from ray origin, in direction * directionX (+ or -), for length rayLength, looking for the collisionmask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector3.right * directionX, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle) {
                    ClimbSlope(ref velocity, slopeAngle);
                }

                velocity.x = (hit.distance - skinWidth) * directionX;
                rayLength = hit.distance;

                // sets the appropriate boolean based on evaluation of directionX == (1 or -1)
                collisions.left = directionX == -1;
                collisions.right = directionX == 1;
            }
        }
    }
    // Detects collisions above and below the entity
    void VerticalCollisions(ref Vector3 velocity) {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for(int i = 0; i < verticalRayCount; i++) {
            // declares and reverses the ray origin based on move direction
            Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            // ray is drawn by vertical spacing
            rayOrigin += Vector3.right * (verticalRaySpacing * i + velocity.x);
            // ray is cast from ray origin, in direction * directionY (+ or -), for length rayLength, stops at collisionMask
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector3.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);

            // hits whichever collider is closest and stops there (prevents clipping)
            if (hit) {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                // sets the appropriate boolean based on evaluation of directionY == (1 or -1)
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
        float moveDistance = Mathf.Abs(velocity.x);
        velocity.y = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
        collisions.below = true;
    }

    // Update the points at which the rays are drawn
    void UpdateRaycastOrigins() {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Origins are set to the corners of the bounding box
        raycastOrigins.topLeft = new Vector3(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector3(bounds.max.x, bounds.max.y);
        raycastOrigins.bottomLeft = new Vector3(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector3(bounds.max.x, bounds.min.y);
    }
    
    // Calculate and update the spacing between the rays
    void CalculateRaySpacing() {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Minumum number of rays is set to 2
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);
        // Spacing divided among number of rays. If the ray count is 2, then the space is the width or height of the whole box
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
    private struct RaycastOrigins {
        public Vector3 topLeft, topRight;
        public Vector3 bottomLeft, bottomRight;
    }
    public struct CollisionInfo {
        public bool above, below;
        public bool left, right;

        public void Reset() {
            above = below = false;
            left = right = false;
        }
    }
}
