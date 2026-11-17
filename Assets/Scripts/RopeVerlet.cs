
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class RopeVerlet : MonoBehaviour
{
    private LineRenderer lr;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    [Header("Rope Settings")]
    [SerializeField] private float ropeSegLen = 0.25f;
    [SerializeField] private int segmentLength = 35;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Vector2 forceGravity = new Vector2(0f, -20f);
    [SerializeField] private int constraintIterations = 50;

    [Header("Rope Anchors")]
    [SerializeField] private Transform playerAnchor;

    [Header("Rope Physics")]
    [SerializeField] private float pullStrength = 20f;
    [SerializeField] private float damping = 2f;

    [Header("Rope Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.05f;
    [SerializeField] private float collisionSkin = 0.01f;

    private Transform attachedHook;
    private Rigidbody2D hookRb;

    private int minSegments = 3;
    private int maxSegments = 100;

    void Start()
    {
        lr = GetComponent<LineRenderer>();

        Vector2 startPosition = GetPlayerPosition();

        // Add the number of segments to the segment list
        for (int i = 0; i < segmentLength; i++)
        {
            ropeSegments.Add(new RopeSegment(startPosition));
            startPosition.y -= ropeSegLen;
        }

        // Set fields of the lineRenderer
        lr.positionCount = ropeSegments.Count;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
    }


    void Update()
    {
        HandleScrollWheel();
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();
        ApplyRopePhysics();
    }

    // Attach the rope to the thrown hook and player
    public void AttachToHook(Transform Hook, Transform player)
    {
        attachedHook = Hook;
        playerAnchor = player;

        hookRb = attachedHook.GetComponent<Rigidbody2D>();

        if (hookRb == null)
        {
            Debug.LogError("Thrown Hook needs a Rigidbody2D!");
            return;
        }

        // Initialize rope between player and hook
        Vector2 start = GetPlayerPosition();
        Vector2 end = attachedHook.position;
        
        // For each ropeSegment get the position of where a segement should start and end to make them all even
        // Lerp stands for Linear Interpolation. In Unity, it lets you calculate a point between two values.

        for (int i = 0; i < ropeSegments.Count; i++)
        {
            Vector2 position = Vector2.Lerp(
                start,
                end,
                (float)i / (ropeSegments.Count - 1)
            );

            ropeSegments[i] = new RopeSegment(position);
        }
    }

    public void DetachFromHook()
    {
        attachedHook = null;
        hookRb = null;
    }

    private Vector2 GetPlayerPosition()
    {
        return playerAnchor != null
            ? playerAnchor.position
            : transform.position;
    }

    // Apply physical force when the rope is stretched
    private void ApplyRopePhysics()
    {
        if (attachedHook == null || hookRb == null)
            return;

        Vector2 start = GetPlayerPosition();
        Vector2 end = attachedHook.position;

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        // Calculate the maximum rope length
        float maxRopeLength = ropeSegLen * (ropeSegments.Count - 1);

        // Only pull when the rope is fully extended
        if (distance > maxRopeLength)
        {
            Vector2 pullDirection = direction.normalized;

            float stretch = distance - maxRopeLength;

            // Calculate hook velocity in the rope's direction
            float velocityAlongRope = Vector2.Dot(
                hookRb.linearVelocity,
                pullDirection
            );

            // Calculate pulling force
            float force = (stretch * pullStrength) +
                          (velocityAlongRope * damping);

            // Pull hook toward player
            hookRb.AddForce(-pullDirection * force);
        }
    }

    private void HandleScrollWheel()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0)
        {
            AddRopeSegment();
        }
        else if (scroll < 0)
        {
            RemoveRopeSegment();
        }
    }

    private void AddRopeSegment()
    {
        if (ropeSegments.Count >= maxSegments)
            return;

        int lastIndex = ropeSegments.Count - 1;

        RopeSegment last = ropeSegments[lastIndex];
        RopeSegment previous = ropeSegments[lastIndex - 1];

        Vector2 newPosition = Vector2.Lerp(
            previous.currPos,
            last.currPos,
            0.5f
        );

        ropeSegments.Insert(
            lastIndex,
            new RopeSegment(newPosition)
        );

        segmentLength = ropeSegments.Count;
        lr.positionCount = segmentLength;
    }

    private void RemoveRopeSegment()
    {
        if (ropeSegments.Count <= minSegments)
            return;

        // Never remove the hook's anchor
        ropeSegments.RemoveAt(ropeSegments.Count - 2);

        segmentLength = ropeSegments.Count;
        lr.positionCount = segmentLength;
    }

    private void Simulate()
    {
        int lastIndex = ropeSegments.Count - 1;

        // Verlet integration
        for (int i = 1; i < ropeSegments.Count; i++)
        {
            if (attachedHook != null && i == lastIndex)
                continue;

            RopeSegment segment = ropeSegments[i];

            Vector2 velocity = segment.currPos - segment.oldPos;

            segment.oldPos = segment.currPos;
            segment.currPos += velocity;

            segment.currPos += forceGravity *
                Time.fixedDeltaTime * Time.fixedDeltaTime;

            ropeSegments[i] = segment;
        }

        // Update player anchor
        RopeSegment first = ropeSegments[0];
        first.currPos = GetPlayerPosition();
        first.oldPos = first.currPos;
        ropeSegments[0] = first;

        // Update Hook anchor
        if (attachedHook != null)
        {
            RopeSegment last = ropeSegments[lastIndex];
            last.currPos = attachedHook.position;
            last.oldPos = last.currPos;
            ropeSegments[lastIndex] = last;
        }

        // Apply distance constraints and collision detection
        for (int i = 0; i < constraintIterations; i++)
        {
            ApplyConstraint();
            ResolveCollisions();
        }
    }

    private void ApplyConstraint()
    {
        int lastIndex = ropeSegments.Count - 1;

        for (int i = 0; i < lastIndex; i++)
        {
            RopeSegment first = ropeSegments[i];
            RopeSegment second = ropeSegments[i + 1];

            Vector2 direction = second.currPos - first.currPos;

            float distance = direction.magnitude;

            if (distance == 0f)
                continue;

            Vector2 correction = direction.normalized *
                (distance - ropeSegLen);

            bool firstIsAnchored = i == 0;
            bool secondIsAnchored =
                attachedHook != null && i + 1 == lastIndex;

            if (firstIsAnchored)
            {
                second.currPos -= correction;
                ropeSegments[i + 1] = second;
            }
            else if (secondIsAnchored)
            {
                first.currPos += correction;
                ropeSegments[i] = first;
            }
            else
            {
                first.currPos += correction * 0.5f;
                second.currPos -= correction * 0.5f;

                ropeSegments[i] = first;
                ropeSegments[i + 1] = second;
            }
        }
    }

    private void DrawRope()
    {
        Vector3[] positions = new Vector3[ropeSegments.Count];

        for (int i = 0; i < ropeSegments.Count; i++)
        {
            positions[i] = ropeSegments[i].currPos;
        }

        lr.positionCount = positions.Length;
        lr.SetPositions(positions);
    }

    private void ResolveCollisions()
{
    for (int i = 1; i < ropeSegments.Count; i++)
    {
        // Don't move the hook anchor
        if (attachedHook != null && i == ropeSegments.Count - 1)
            continue;

        RopeSegment segment = ropeSegments[i];

        Collider2D hit = Physics2D.OverlapCircle(
            segment.currPos,
            collisionRadius,
            collisionMask
        );

        if (hit != null)
        {
            // Find the closest point on the collider
            Vector2 closestPoint = hit.ClosestPoint(segment.currPos);

            Vector2 direction = segment.currPos - closestPoint;

            // Calculate the direction to push the segment out
            if (direction.sqrMagnitude < 0.0001f)
            {
                Vector2 fallbackDirection =
                    (Vector2)hit.bounds.center - segment.currPos;

                direction = -fallbackDirection.normalized;

                if (direction.sqrMagnitude < 0.0001f)
                    direction = Vector2.up;
            }
            else
            {
                direction.Normalize();
            }

            // Move the segment outside the collider
            Vector2 surfacePoint = closestPoint;

            segment.currPos = surfacePoint +
                direction * (collisionRadius + collisionSkin);

            // Prevent velocity from pushing the segment back into the object
            Vector2 velocity = segment.currPos - segment.oldPos;

            float velocityIntoSurface = Vector2.Dot(
                velocity,
                -direction
            );

            if (velocityIntoSurface > 0)
            {
                velocity -= -direction * velocityIntoSurface;
            }

            segment.oldPos = segment.currPos - velocity;

            ropeSegments[i] = segment;
        }
    }
}

    public struct RopeSegment
    {
        public Vector2 currPos;
        public Vector2 oldPos;

        public RopeSegment(Vector2 pos)
        {
            currPos = pos;
            oldPos = pos;
        }
    }
}
