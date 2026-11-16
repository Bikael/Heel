
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RopeVerlet : MonoBehaviour
{
    private LineRenderer lr;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    private float ropeSegLen = 0.25f;
    private int segmentLength = 35;
    private float lineWidth = 0.1f;

    private Vector2 forceGravity = new Vector2(0f, -1f);
    private int constraintIterations = 50;

    void Start()
    {
        lr = GetComponent<LineRenderer>();

        Vector3 ropeStartPoint = Vector3.zero;

        for (int i = 0; i < segmentLength; i++)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= ropeSegLen;
        }

        lr.positionCount = segmentLength;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
    }

    void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();
    }

    private void Simulate()
    {
        for (int i = 1; i < segmentLength; i++)
        {
            // Verlet integration
            RopeSegment firstSegment = ropeSegments[i];

            Vector2 velocity = firstSegment.currPos - firstSegment.oldPos;

            firstSegment.oldPos = firstSegment.currPos;

            firstSegment.currPos += velocity;

            firstSegment.currPos += forceGravity *
                Time.fixedDeltaTime * Time.fixedDeltaTime;

            ropeSegments[i] = firstSegment;
        }

        // Apply distance constraints
        for (int i = 0; i < constraintIterations; i++)
        {
            ApplyConstraint();
        }
    }

    private void ApplyConstraint()
    {
        // First segment follows mouse position
        RopeSegment firstSegment = ropeSegments[0];

        Vector3 mousePosition = Mouse.current.position.ReadValue();
        mousePosition.z = 10f;

        firstSegment.currPos =
            Camera.main.ScreenToWorldPoint(mousePosition);

        ropeSegments[0] = firstSegment;

        for (int i = 0; i < segmentLength - 1; i++)
        {
            RopeSegment firstSeg = ropeSegments[i];
            RopeSegment secondSeg = ropeSegments[i + 1];

            float dist = (firstSeg.currPos - secondSeg.currPos).magnitude;

            float error = Mathf.Abs(dist - ropeSegLen);

            Vector2 changeDir = Vector2.zero;

            if (dist > ropeSegLen)
            {
                changeDir = (firstSeg.currPos - secondSeg.currPos).normalized;
            }
            else if (dist < ropeSegLen)
            {
                changeDir = (secondSeg.currPos - firstSeg.currPos).normalized;
            }

            Vector2 changeAmount = changeDir * error;

            if (i != 0)
            {
                // Move both segments equally
                firstSeg.currPos -= changeAmount * 0.5f;
                secondSeg.currPos += changeAmount * 0.5f;

                ropeSegments[i] = firstSeg;
                ropeSegments[i + 1] = secondSeg;
            }
            else
            {
                // First segment is anchored
                secondSeg.currPos += changeAmount;

                ropeSegments[i + 1] = secondSeg;
            }
        }
    }

    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[segmentLength];

        for (int i = 0; i < segmentLength; i++)
        {
            ropePositions[i] = ropeSegments[i].currPos;
        }

        lr.SetPositions(ropePositions);
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
