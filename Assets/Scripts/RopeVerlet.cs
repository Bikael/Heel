
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class RopeVerlet : MonoBehaviour
{
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    [Header("Rope Settings")]
    [SerializeField] private LineRenderer lr;
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

    private bool ropeActive = false;

    public bool HasMinimumSegments => ropeSegments.Count <= minSegments;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();

        lr.enabled = true;
        lr.useWorldSpace = true;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.startColor = Color.white;
        lr.endColor = Color.white;

        lr.positionCount = 0;
        lr.sortingOrder = 10;

        if (lr.sharedMaterial == null)
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void CreateLeash(Transform player)
    {
        Debug.Log("Creating rope!");

        if (player == null)
        {
            Debug.LogError("Player anchor is missing!");
            return;
        }

        DestroyLeash();

        playerAnchor = player;
        ropeActive = true;

        Vector2 startPosition = GetPlayerPosition();

        for (int i = 0; i < segmentLength; i++)
        {
            ropeSegments.Add(new RopeSegment(startPosition));
        }

        lr.positionCount = ropeSegments.Count;

        Debug.Log("Rope segments created: " + ropeSegments.Count);
    }

    public void DestroyLeash()
    {
        ropeActive = false;

        ropeSegments.Clear();

        attachedHook = null;
        hookRb = null;

        if (lr != null)
        {
            lr.positionCount = 0;
        }
    }

    private void Update()
    {
        if (!ropeActive || ropeSegments.Count < 2)
            return;

        HandleScrollWheel();
        DrawRope();
    }

    private void FixedUpdate()
    {
        if (!ropeActive || ropeSegments.Count < 2)
            return;

        Simulate();
        ApplyRopePhysics();
    }

    public void AttachToHook(Transform hook, Transform player)
    {
        if (!ropeActive || ropeSegments.Count < 2)
        {
            Debug.LogError("Cannot attach hook: Rope has not been created.");
            return;
        }

        if (hook == null || player == null)
        {
            Debug.LogError("Cannot attach hook: Missing transform reference.");
            return;
        }

        attachedHook = hook;
        playerAnchor = player;

        hookRb = attachedHook.GetComponent<Rigidbody2D>();

        if (hookRb == null)
        {
            Debug.LogError("Thrown Hook needs a Rigidbody2D!");
            attachedHook = null;
            return;
        }

        Vector2 start = GetPlayerPosition();
        Vector2 end = attachedHook.position;

        for (int i = 0; i < ropeSegments.Count; i++)
        {
            Vector2 position = Vector2.Lerp(
                start,
                end,
                (float)i / (ropeSegments.Count - 1)
            );

            ropeSegments[i] = new RopeSegment(position);
        }

        DrawRope();
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

    private void ApplyRopePhysics()
    {
        if (attachedHook == null || hookRb == null)
            return;

        Vector2 start = GetPlayerPosition();
        Vector2 end = attachedHook.position;

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        float maxRopeLength = ropeSegLen * (ropeSegments.Count - 1);

        if (distance > maxRopeLength)
        {
            Vector2 pullDirection = direction.normalized;

            float stretch = distance - maxRopeLength;

            float velocityAlongRope = Vector2.Dot(
                hookRb.linearVelocity,
                pullDirection
            );

            float force = (stretch * pullStrength) +
                          (velocityAlongRope * damping);

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
        if (!ropeActive || ropeSegments.Count < 2)
            return;

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

    public void RemoveRopeSegment()
    {
        if (!ropeActive || ropeSegments.Count <= minSegments)
            return;

        // Preserve player and hook anchor segments
        ropeSegments.RemoveAt(ropeSegments.Count - 2);

        segmentLength = ropeSegments.Count;
        lr.positionCount = segmentLength;

        DrawRope();
    }

    private void Simulate()
    {
        if (ropeSegments.Count < 2)
            return;

        int lastIndex = ropeSegments.Count - 1;

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

        RopeSegment first = ropeSegments[0];

        first.currPos = GetPlayerPosition();
        first.oldPos = first.currPos;

        ropeSegments[0] = first;

        if (attachedHook != null)
        {
            RopeSegment last = ropeSegments[lastIndex];

            last.currPos = attachedHook.position;
            last.oldPos = last.currPos;

            ropeSegments[lastIndex] = last;
        }

        for (int i = 0; i < constraintIterations; i++)
        {
            ApplyConstraint();
            ResolveCollisions();
        }
    }

    private void ApplyConstraint()
    {
        if (ropeSegments.Count < 2)
            return;

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
        if (lr == null || ropeSegments.Count == 0)
            return;

        lr.positionCount = ropeSegments.Count;

        for (int i = 0; i < ropeSegments.Count; i++)
        {
            lr.SetPosition(i, ropeSegments[i].currPos);
        }
    }

    private void ResolveCollisions()
    {
        for (int i = 1; i < ropeSegments.Count; i++)
        {
            if (attachedHook != null && i == ropeSegments.Count - 1)
                continue;

            RopeSegment segment = ropeSegments[i];

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                segment.currPos,
                collisionRadius,
                collisionMask
            );

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                Vector2 closestPoint = hit.ClosestPoint(segment.currPos);

                Vector2 direction = segment.currPos - closestPoint;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    Bounds bounds = hit.bounds;

                    float left = Mathf.Abs(segment.currPos.x - bounds.min.x);
                    float right = Mathf.Abs(bounds.max.x - segment.currPos.x);
                    float bottom = Mathf.Abs(segment.currPos.y - bounds.min.y);
                    float top = Mathf.Abs(bounds.max.y - segment.currPos.y);

                    float nearest = Mathf.Min(left, right, bottom, top);

                    if (nearest == left)
                        direction = Vector2.left;
                    else if (nearest == right)
                        direction = Vector2.right;
                    else if (nearest == bottom)
                        direction = Vector2.down;
                    else
                        direction = Vector2.up;

                    if (direction == Vector2.left)
                        segment.currPos.x = bounds.min.x - collisionRadius - collisionSkin;
                    else if (direction == Vector2.right)
                        segment.currPos.x = bounds.max.x + collisionRadius + collisionSkin;
                    else if (direction == Vector2.down)
                        segment.currPos.y = bounds.min.y - collisionRadius - collisionSkin;
                    else
                        segment.currPos.y = bounds.max.y + collisionRadius + collisionSkin;
                }
                else
                {
                    direction.Normalize();

                    segment.currPos = closestPoint +
                        direction * (collisionRadius + collisionSkin);
                }

                Vector2 velocity = segment.currPos - segment.oldPos;

                float velocityIntoSurface = Vector2.Dot(
                    velocity,
                    -direction
                );

                if (velocityIntoSurface > 0f)
                {
                    velocity -= -direction * velocityIntoSurface;
                }

                segment.oldPos = segment.currPos - velocity;
            }

            ropeSegments[i] = segment;
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
