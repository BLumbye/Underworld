using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class Controller2D : RaycastController {
    [HideInInspector] public CollisionInfo collisions;

    private bool shouldFallThroughPlatform = false;

    public override void Start() {
        base.Start();
        collisions.faceDir = 1;
    }


    public void Move(Vector2 moveAmount, bool standingOnPlatform = false) {
        UpdateRaycastOrigins();
        collisions.Reset();

        if (moveAmount.x != 0) {
            collisions.faceDir = (int) Mathf.Sign(moveAmount.x);
        }

        HorizontalCollisions(ref moveAmount);

        if (moveAmount.y != 0) {
            VerticalCollisions(ref moveAmount);
        }

        transform.Translate(moveAmount);

        if (standingOnPlatform) {
            collisions.below = true;
        }

        shouldFallThroughPlatform = false;
    }

    void HorizontalCollisions(ref Vector2 moveAmount) {
        // Send out rays horizontally in the direction you are facing
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        if (Mathf.Abs(moveAmount.x) < skinWidth) {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++) {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            if (hit) {
                // If the ground is inside you, ignore it
                if (hit.distance == 0) {
                    continue;
                }

                // Set the horizontal move amount to the distance to the object
                moveAmount.x = Mathf.Min(Mathf.Abs(moveAmount.x), hit.distance - skinWidth) * directionX;
                rayLength = Mathf.Min(Mathf.Abs(moveAmount.x) + skinWidth, hit.distance);

                collisions.left = directionX == -1;
                collisions.right = directionX == 1;
            }
        }
    }

    void VerticalCollisions(ref Vector2 moveAmount) {
        // Send out rays vertically in the direction you are moving
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++) {
            Vector2 rayOrigin = directionY == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

            if (hit) {
                // BUG: Long platforms and moving platforms have problems with falling through
                if (hit.collider.tag == "Hollow") {
                    collisions.standingOnHollow = true;

                    if (directionY == 1 || hit.distance == 0) {
                        continue;
                    }

                    if (collisions.fallingThroughPlatform) {
                        continue;
                    }

                    if (shouldFallThroughPlatform) {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", 0.5f);
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    public void FallThroughPlatform() {
        shouldFallThroughPlatform = true;
    }

    void ResetFallingThroughPlatform() {
        collisions.fallingThroughPlatform = false;
    }

    public struct CollisionInfo {
        public bool above, below, left, right;

        public int faceDir;
        public bool fallingThroughPlatform;
        public bool standingOnHollow;

        public void Reset() {
            above = below = left = right = false;
            standingOnHollow = false;
        }
    }
}
