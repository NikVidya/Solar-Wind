using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider))]
public class Controller : MonoBehaviour {

    static float skinWidth = .15f;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    private float horizontalRaySpacing, verticalRaySpacing;

    private BoxCollider collider;
    private RaycastOrigins raycastOrigins;
	void Start () {
        collider = GetComponent<BoxCollider>();
	}
	
	void Update () {
        UpdateRaycastOrigins();
        CalculateRaySpacing();
        for (int i = 0; i < verticalRayCount; i++) {
            Debug.DrawRay(raycastOrigins.bottomLeft + Vector3.right * verticalRaySpacing * i, Vector3.up * -2, Color.red);
        }
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
    struct RaycastOrigins {
        public Vector3 topLeft, topRight;
        public Vector3 bottomLeft, bottomRight;
    }
}
