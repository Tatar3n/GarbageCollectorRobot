using UnityEngine;
using System.Collections.Generic;

namespace Fuzzy
{
    public class FuzzyLogicSystem : MonoBehaviour
    {
        [Header("Sensors")]
        public Transform frontSensor;
        public Transform back1Sensor;
        public Transform back2Sensor;

        [Header("Movement Settings")]
        public float speed = 3f;
        public float detectionRadius = 5f;
        public float obstacleAvoidDistance = 1.5f;

        [Header("Goal / Search (не влияет на fuzzy-формулы)")]
        public bool enableGoalSeeking = true;
        public float wanderRadius = 2.5f;
        public float wanderPointReachDistance = 0.25f;
        public Vector2 wanderRepathTimeRange = new Vector2(1f, 3f);
        public bool seekTrashBinWhenCarrying = true;
        public float trashBinRefreshInterval = 0.75f;
        public bool keepWanderPointsOutOfObstacles = true;
        public int wanderPickAttempts = 12;

        [Header("Smoothing")]
        public float directionSmoothing = 10f;
        public float speedSmoothing = 8f;
        public float maxAcceleration = 20f;

        public LayerMask obstacleLayer;
        public bool showDebug = true;

        private readonly FuzzyFunction fuzzyFunction = new FuzzyFunction();
        private Rigidbody2D rb;
        private Vector2 movementDirection = Vector2.zero;
        private Vector2 smoothedMoveDirection = Vector2.zero;
        private float targetSpeed = 0f;
        private Vector2 desiredVelocity = Vector2.zero;

        // High-level movement target (search/delivery). Fuzzy logic below stays intact.
        private Inventory inventory;
        private Types.GType lastGarbageType = Types.GType.None;
        private TrashBin cachedTargetBin;
        private float nextBinRefreshTime;
        private Vector2 currentWanderPoint;
        private bool hasWanderPoint;
        private float nextWanderPickTime;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            // Отключаем ручное управление, чтобы не конфликтовало с ИИ-движением.
            Move manualMove = GetComponent<Move>();
            if (manualMove != null)
            {
                manualMove.enabled = false;
            }

            inventory = GetComponent<Inventory>();
            smoothedMoveDirection = Vector2.right;
            targetSpeed = speed;
            PickNewWanderPoint();
        }

        void Update()
        {
            CalculateMovement();
            SmoothMovementAndSpeed();
            ApplyRotation();
        }

        void FixedUpdate()
        {
            Vector2 moveDirForPhysics = smoothedMoveDirection;

            if (moveDirForPhysics.magnitude > 0.1f)
            {
                desiredVelocity = moveDirForPhysics.normalized * speed;
            }
            else
            {
                desiredVelocity = Vector2.zero;
            }

            if (rb != null)
            {
                rb.velocity = Vector2.MoveTowards(rb.velocity, desiredVelocity, maxAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                transform.position += (Vector3)(desiredVelocity * Time.fixedDeltaTime);
            }
        }

        void CalculateMovement()
        {
            Vector2 baseDesiredDirection = GetBaseDesiredDirection();

            // Нечёткая логика для поворота (по боковым датчикам)
            float dRight = back1Sensor ? CheckObstacleDistance(back1Sensor) : float.MaxValue;
            float dLeft = back2Sensor ? CheckObstacleDistance(back2Sensor) : float.MaxValue;
            float dMin = 0.0f;
            bool isLeft = true;

            if (Mathf.Abs(dLeft - dRight) > 0.5f)
            {
                dMin = Mathf.Min(dLeft, dRight);
                isLeft = dLeft <= dRight;
            }
            else
            {
                dMin = Mathf.Min(dLeft, dRight);
                isLeft = true;
            }

            float turnAngle = fuzzyFunction.Sentr_mass_rotate(dMin, isLeft);
            Debug.Log(dMin);
            Debug.Log(turnAngle);

            Vector2 forward;
            if (enableGoalSeeking && baseDesiredDirection.sqrMagnitude > 0.0001f)
            {
                forward = baseDesiredDirection.normalized;
            }
            else
            {
                forward = (smoothedMoveDirection.sqrMagnitude > 0.0001f ? smoothedMoveDirection : Vector2.right).normalized;
            }
            Vector2 turned = (Vector2)(Quaternion.Euler(0f, 0f, turnAngle) * forward);

            if (turned.sqrMagnitude > 0.0001f)
            {
                movementDirection = turned.normalized;
            }
            else
            {
                movementDirection = forward;
            }

            // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ (по направлению движения, чтобы "остановка" работала даже при смене цели)
            float frontDist = CheckObstacleDistanceInDirection(frontSensor ? (Vector2)frontSensor.position : (Vector2)transform.position, movementDirection);
            targetSpeed = fuzzyFunction.Sentr_mass(frontDist);
        }

        void SmoothMovementAndSpeed()
        {
            float dt = Time.deltaTime;
            float dirAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * dt);
            float spdAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, speedSmoothing) * dt);

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, movementDirection.normalized, dirAlpha);
            }
            else
            {
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, Vector2.zero, dirAlpha);
            }

            speed = Mathf.Lerp(speed, targetSpeed, spdAlpha);
            speed = Mathf.Max(0f, speed);
        }

        void ApplyRotation()
        {
            if (smoothedMoveDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(smoothedMoveDirection.y, smoothedMoveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        Vector2 GetSensorWorldDirection(Transform sensor)
        {
            if (sensor == null) return Vector2.zero;
            Vector2 fromCenter = (Vector2)sensor.position - (Vector2)transform.position;
            if (fromCenter.sqrMagnitude < 0.0001f) return Vector2.zero;
            return fromCenter.normalized;
        }

        float CheckObstacleDistance(Transform sensor)
        {
            if (sensor == null) return float.MaxValue;
            Vector2 dir = GetSensorWorldDirection(sensor);
            if (dir.sqrMagnitude < 0.0001f) return float.MaxValue;

            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, obstacleAvoidDistance * 2f, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(sensor.position, dir * obstacleAvoidDistance * 2f, hit.collider ? Color.red : Color.green);
            }
            return hit.collider ? hit.distance : float.MaxValue;
        }

        float CheckObstacleDistanceInDirection(Vector2 origin, Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return float.MaxValue;
            Vector2 nd = dir.normalized;
            float castDist = obstacleAvoidDistance * 2f;
            RaycastHit2D hit = Physics2D.Raycast(origin, nd, castDist, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(origin, nd * castDist, hit.collider ? Color.magenta : Color.cyan);
            }
            return hit.collider ? hit.distance : float.MaxValue;
        }

        private Vector2 GetBaseDesiredDirection()
        {
            if (!enableGoalSeeking)
            {
                return Vector2.zero;
            }

            // If Inventory isn't present – fallback to wander.
            Types.GType carried = Types.GType.None;
            if (inventory != null)
            {
                carried = inventory.getCell();
            }

            // If carrying garbage -> go to matching trash bin.
            if (seekTrashBinWhenCarrying && carried != Types.GType.None)
            {
                if (carried != lastGarbageType)
                {
                    lastGarbageType = carried;
                    cachedTargetBin = null;
                    nextBinRefreshTime = 0f;
                }

                if (Time.time >= nextBinRefreshTime || cachedTargetBin == null || cachedTargetBin.garbageType != carried)
                {
                    cachedTargetBin = FindClosestTrashBin(carried);
                    nextBinRefreshTime = Time.time + Mathf.Max(0.05f, trashBinRefreshInterval);
                }

                if (cachedTargetBin != null)
                {
                    Vector2 toBin = (Vector2)cachedTargetBin.transform.position - (Vector2)transform.position;
                    return toBin;
                }
            }
            else
            {
                lastGarbageType = Types.GType.None;
                cachedTargetBin = null;
            }

            // Search mode: wander to random points near the robot.
            if (!hasWanderPoint || Time.time >= nextWanderPickTime)
            {
                PickNewWanderPoint();
            }

            Vector2 toPoint = currentWanderPoint - (Vector2)transform.position;
            if (toPoint.magnitude <= Mathf.Max(0.01f, wanderPointReachDistance))
            {
                PickNewWanderPoint();
                toPoint = currentWanderPoint - (Vector2)transform.position;
            }

            return toPoint;
        }

        private void PickNewWanderPoint()
        {
            Vector2 center = transform.position;
            float r = Mathf.Max(0.01f, wanderRadius);
            Vector2 chosen = center + Random.insideUnitCircle * r;

            if (keepWanderPointsOutOfObstacles)
            {
                int attempts = Mathf.Max(1, wanderPickAttempts);
                for (int i = 0; i < attempts; i++)
                {
                    Vector2 candidate = center + Random.insideUnitCircle * r;
                    bool insideObstacle = Physics2D.OverlapCircle(candidate, 0.05f, obstacleLayer) != null;
                    bool blocked = Physics2D.Linecast(center, candidate, obstacleLayer).collider != null;
                    if (!insideObstacle && !blocked)
                    {
                        chosen = candidate;
                        break;
                    }
                }
            }

            currentWanderPoint = chosen;
            hasWanderPoint = true;

            float minT = Mathf.Max(0.05f, wanderRepathTimeRange.x);
            float maxT = Mathf.Max(minT, wanderRepathTimeRange.y);
            nextWanderPickTime = Time.time + Random.Range(minT, maxT);
        }

        private TrashBin FindClosestTrashBin(Types.GType type)
        {
            TrashBin[] bins = FindObjectsOfType<TrashBin>();
            TrashBin best = null;
            float bestDistSq = float.PositiveInfinity;
            Vector2 pos = transform.position;

            for (int i = 0; i < bins.Length; i++)
            {
                TrashBin b = bins[i];
                if (b == null || b.garbageType != type) continue;
                float d = ((Vector2)b.transform.position - pos).sqrMagnitude;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = b;
                }
            }

            return best;
        }
    }
}