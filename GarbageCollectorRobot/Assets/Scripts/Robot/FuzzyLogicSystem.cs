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

        [Header("Goal / Search")]
        public bool enableGoalSeeking = true;
        public float wanderRadius = 2.5f;
        public float wanderPointReachDistance = 0.25f;
        public Vector2 wanderRepathTimeRange = new Vector2(1f, 3f);
        public bool seekTrashBinWhenCarrying = true;
        public float trashBinRefreshInterval = 0.75f;

        [Header("Trash Seeking")]
        public bool enableTrashSeeking = true;
        public float trashDetectDistance = 6f;
        public float trashMemorySeconds = 1.25f;
        public float trashMaxChaseDistance = 10f;
        public bool showTrashDebug = true;

        public bool enableScan = true;
        public float scanIntervalSeconds = 2f;
        public float scanDurationSeconds = 0.8f;
        public float scanRotationSpeedDegPerSec = 540f;

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

        private Inventory inventory;
        private Types.GType lastGarbageType = Types.GType.None;
        private TrashBin cachedTargetBin;
        private float nextBinRefreshTime;
        private Vector2 currentWanderPoint;
        private bool hasWanderPoint;
        private float nextWanderPickTime;

        private KeepGarbage currentTrashTarget;
        private float lastTrashSeenTime;
        private float nextScanTime;
        private bool isScanning;
        private float scanEndTime;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            Move manualMove = GetComponent<Move>();
            if (manualMove != null)
            {
                manualMove.enabled = false;
            }

            inventory = GetComponent<Inventory>();
            smoothedMoveDirection = Vector2.right;
            targetSpeed = speed;
            PickNewWanderPoint();

            nextScanTime = Time.time + Mathf.Max(0.1f, scanIntervalSeconds);
        }

        void Update()
        {
            UpdateTrashSeekingAndScanState();

            if (isScanning)
            {
                UpdateScanRotationAndDetectTrash();
                return;
            }

            CalculateMovement();
            SmoothMovementAndSpeed();
            ApplyRotation();
        }

        void FixedUpdate()
        {
            if (isScanning)
            {
                if (rb != null)
                {
                    rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, maxAcceleration * Time.fixedDeltaTime);
                }
                return;
            }

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

            float frontDist = CheckObstacleDistance(frontSensor);
            float leftDist = CheckObstacleDistance(back1Sensor);
            float rightDist = CheckObstacleDistance(back2Sensor);
            targetSpeed = fuzzyFunction.Sentr_mass(Mathf.Min(frontDist, leftDist, rightDist));

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

            float turnAngle = fuzzyFunction.Sentr_mass_rotate(dMin, isLeft, targetSpeed);
            if (turnAngle > -1001 && turnAngle < -999)
            {
                transform.rotation = Quaternion.Euler(0, 0, 90f);
            }
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

        private Vector2 GetBaseDesiredDirection()
        {
            if (!enableGoalSeeking)
            {
                return Vector2.zero;
            }

            Types.GType carried = Types.GType.None;
            if (inventory != null)
            {
                carried = inventory.getCell();
            }

            if (enableTrashSeeking && carried == Types.GType.None && currentTrashTarget != null)
            {
                Vector2 toTrash = (Vector2)currentTrashTarget.transform.position - (Vector2)transform.position;
                return toTrash;
            }

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
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0.01f, wanderRadius);
            currentWanderPoint = center + offset;
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

        private void UpdateTrashSeekingAndScanState()
        {
            if (!enableTrashSeeking && !enableScan)
            {
                return;
            }

            Types.GType carried = Types.GType.None;
            if (inventory != null)
            {
                carried = inventory.getCell();
            }

            if (carried != Types.GType.None)
            {
                currentTrashTarget = null;
                isScanning = false;
                return;
            }

            if (currentTrashTarget == null)
            {
            }
            else
            {
                float dist = Vector2.Distance(transform.position, currentTrashTarget.transform.position);
                if (!currentTrashTarget.gameObject.activeInHierarchy || dist > Mathf.Max(0.1f, trashMaxChaseDistance))
                {
                    currentTrashTarget = null;
                }
            }

            KeepGarbage seen = TryGetTrashInFront();
            if (seen != null)
            {
                currentTrashTarget = seen;
                lastTrashSeenTime = Time.time;
                isScanning = false;
                return;
            }

            if (currentTrashTarget != null && (Time.time - lastTrashSeenTime) > Mathf.Max(0.05f, trashMemorySeconds))
                currentTrashTarget = null;

            if (enableScan && currentTrashTarget == null && !isScanning && Time.time >= nextScanTime)
                StartScan();
        }

        private void StartScan()
        {
            isScanning = true;
            scanEndTime = Time.time + Mathf.Max(0.05f, scanDurationSeconds);
            nextScanTime = Time.time + Mathf.Max(0.1f, scanIntervalSeconds);
        }

        private void UpdateScanRotationAndDetectTrash()
        {
            float dt = Time.deltaTime;
            float dz = scanRotationSpeedDegPerSec * dt;
            transform.rotation = Quaternion.Euler(0f, 0f, transform.eulerAngles.z + dz);

            KeepGarbage seen = TryGetTrashInFront();
            if (seen != null)
            {
                currentTrashTarget = seen;
                lastTrashSeenTime = Time.time;
                isScanning = false;
                return;
            }

            if (Time.time >= scanEndTime)
                isScanning = false;
        }

        private KeepGarbage TryGetTrashInFront()
        {
            Vector2 origin = frontSensor != null ? (Vector2)frontSensor.position : (Vector2)transform.position;
            Vector2 dir = (Vector2)transform.up;
            if (dir.sqrMagnitude < 0.0001f) return null;

            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = false
            };

            RaycastHit2D[] hits = new RaycastHit2D[16];
            int count = Physics2D.Raycast(origin, dir, filter, hits, Mathf.Max(0.1f, trashDetectDistance));
            if (count <= 0)
            {
                if (showTrashDebug)
                {
                    Debug.DrawRay(origin, dir * trashDetectDistance, new Color(1f, 0.9f, 0.2f, 0.6f));
                }
                return null;
            }

            float nearestObstacle = float.PositiveInfinity;
            KeepGarbage nearestTrash = null;
            float nearestTrashDist = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                float d = hits[i].distance;

                bool isObstacle = ((1 << col.gameObject.layer) & obstacleLayer.value) != 0;
                if (isObstacle)
                {
                    if (d < nearestObstacle) nearestObstacle = d;
                    continue;
                }

                if (!col.CompareTag("garbage")) continue;

                if (col.TryGetComponent<KeepGarbage>(out var garbage))
                {
                    if (d < nearestTrashDist)
                    {
                        nearestTrashDist = d;
                        nearestTrash = garbage;
                    }
                }
            }

            if (nearestTrash != null && nearestTrashDist < nearestObstacle)
            {
                if (showTrashDebug)
                {
                    Debug.DrawRay(origin, dir * nearestTrashDist, Color.yellow);
                }
                return nearestTrash;
            }

            if (showTrashDebug)
            {
                Debug.DrawRay(origin, dir * trashDetectDistance, new Color(1f, 0.5f, 0.2f, 0.6f));
            }
            return null;
        }
    }
}