using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RopeVerlet : MonoBehaviour
{
    [SerializeField] private Transform _anchorObject;
    [Header("Rope")]
    [SerializeField] private int _numberOfRopeSegments = 50;
    [SerializeField] private float _ropeSegmentsLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0f, -2f);
    [SerializeField] private float _dampingFactor = 0.98f;
    private LineRenderer _lineRenderer;
    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.1f;
    [SerializeField] private float _correctionClampAmount = 0.1f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 50;

    [Header("Optimizations")]
    [SerializeField] private int _collisionSegmentsInterval = 2;

    private Vector3 _ropeStartPoint;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _numberOfRopeSegments;

        if (_anchorObject == null)
        {
            _anchorObject = this.transform;
        }
        // _ropeStartPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadDefaultValue());
        Vector3 ropeStartPoint = _anchorObject.position;

        for (int i = 0; i < _numberOfRopeSegments; ++i)
        {
            _ropeSegments.Add(new RopeSegment(_ropeStartPoint));
            _ropeStartPoint.y -= _ropeSegmentsLength;
        }
    }


    private void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < _numOfConstraintRuns; ++i)
        {
            ApplyCostraints();

            if (i % _collisionSegmentsInterval == 0)
                HandleCollisions();
        }

    }


    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_numberOfRopeSegments];
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            ropePositions[i] = _ropeSegments[i].CurrentPosition;


        }

        _lineRenderer.SetPositions(ropePositions);
    }
    private void Simulate()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)

        {
            RopeSegment segment = _ropeSegments[i];
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;

            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += _gravityForce * Time.fixedDeltaTime;
            _ropeSegments[i] = segment;
        }


    }

    private void ApplyCostraints()

    {
        RopeSegment firstSegment = _ropeSegments[0];
        firstSegment.CurrentPosition = _anchorObject.position;
        _ropeSegments[0] = firstSegment;

        for (int i = 0; i < _numberOfRopeSegments - 1; i++)
        {
            RopeSegment currentSeg = _ropeSegments[i];
            RopeSegment nextSeg = _ropeSegments[i + 1];


            float dist = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).magnitude;
            float difference = (dist - _ropeSegmentsLength);

            Vector2 changeDir = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).normalized;
            Vector2 changeVector = changeDir * difference;


            if (i != 0)
            {
                currentSeg.CurrentPosition -= (changeVector * 0.5f);
                nextSeg.CurrentPosition += (changeVector * 0.5f);

            }
            else
            {
                nextSeg.CurrentPosition += changeVector;

            }
            _ropeSegments[i] = currentSeg;
            _ropeSegments[i + 1] = nextSeg;
        }

    }
    private void HandleCollisions()
    {
        for (int i = 1; i < _ropeSegments.Count; i++)

        {
            RopeSegment segment = _ropeSegments[i];
            Vector2 velocity = segment.CurrentPosition - segment.OldPosition;
            Collider2D[] coliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, _collisionRadius, _collisionMask);

            foreach (Collider2D colider in coliders)
            {
                Vector2 closestPoint = colider.ClosestPoint(segment.CurrentPosition);
                float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);

                if (distance < _collisionRadius)
                {
                    Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;

                    if (normal == Vector2.zero)
                    {

                        normal = (segment.CurrentPosition - (Vector2)colider.transform.position).normalized;
                    }

                    float depth = _collisionRadius - distance;
                    segment.CurrentPosition += normal * depth;

                    velocity = Vector2.Reflect(velocity, normal) * _bounceFactor;

                }

            }
            segment.OldPosition = segment.CurrentPosition - velocity;
            _ropeSegments[i] = segment;
        }
    }
    public struct RopeSegment
    {
        public Vector2 CurrentPosition;
        public Vector2 OldPosition;

        public RopeSegment(Vector2 pos)
        {

            CurrentPosition = pos;
            OldPosition = pos;
        }
    }
}