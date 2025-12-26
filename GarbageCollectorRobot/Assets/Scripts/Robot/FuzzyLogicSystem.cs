using UnityEngine;
using System.Collections.Generic;

namespace Fuzzy
{
    public class FuzzyLogicSystem : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 3f;
        [Tooltip("Upper speed limit. If 0, defaults to initial 'speed'.")]
        public float maxMoveSpeed = 0f;
        [Tooltip("Lower speed limit.")]
        public float minMoveSpeed = 0f;
        public float detectionRadius = 5f;
        public float obstacleAvoidDistance = 1.5f;
        public float rotationInterval = 5f;
        public float safeDistance = 0.3f;
        public float rotationDuration = 1f;
        public float safeZoneDistance = 2f;
        public float boundaryCooldown = 1f;

        [Header("Fuzzy control (sensors -> steering + speed)")]
        [Tooltip("Max steering angle (deg) produced by fuzzy steering.")]
        [Range(0f, 90f)]
        public float maxSteerAngleDeg = 70f;
        [Tooltip("Overall fuzzy steering gain. Higher = more aggressive turns away from obstacles.")]
        public float steerGain = 1.2f;
        [Tooltip("Weights: how strongly each sensor contributes to left/right fuzzy 'obstacle' degrees.")]
        public float weightFront = 1.25f;
        public float weightFrontDiag = 1.0f;
        public float weightSide = 0.9f;
        public float weightBack = 0.25f;

        [Header("Steering / Anti-jitter")]
        [Tooltip("How fast direction changes are smoothed. Higher = snappier, lower = smoother.")]
        public float directionSmoothing = 10f;
        [Tooltip("How fast speed changes are smoothed (for fuzzy output).")]
        public float speedSmoothing = 8f;
        [Tooltip("Max acceleration applied to Rigidbody2D velocity (reduces shaking).")]
        public float maxAcceleration = 20f;

        [Header("Obstacle avoidance")]
        [Tooltip("Weight of pushing away from obstacle normals.")]
        public float avoidanceWeight = 1.2f;
        [Tooltip("Weight of tangential (sideways) steering along obstacles.")]
        public float tangentWeight = 1.0f;
        [Tooltip("Seconds to keep chosen side when avoiding (prevents left-right twitch).")]
        public float avoidSideLockTime = 0.8f;
        [Tooltip("Distance factor to consider path 'clear' to drop the avoidance lock.")]
        public float clearFactor = 1.15f;

        [Header("Turn safety (prevents corner-cut collisions)")]
        [Tooltip("Reduce speed on sharp turns. 1 = no slow, 0.3 = strong slow.")]
        [Range(0.2f, 1f)]
        public float sharpTurnSpeedFactor = 0.45f;
        [Tooltip("Angle (deg) where turn slow starts.")]
        public float turnSlowStartAngle = 20f;
        [Tooltip("Angle (deg) where max turn slow is reached.")]
        public float turnSlowFullAngle = 80f;
        [Tooltip("If an obstacle is closer than this in front, steering snaps (no smoothing) and speed is clamped.")]
        public float emergencySnapDistance = 0.22f;

        [Header("Boundary / wall interaction")]
        [Tooltip("Boundary reactions only trigger if we are moving INTO the wall (dot(vel, normal) < -threshold).")]
        [Range(0f, 1f)]
        public float boundaryApproachThreshold = 0.15f;

        [Header("Anti-stuck")]
        [Tooltip("How often we check if the robot is stuck.")]
        public float stuckCheckInterval = 0.35f;
        [Tooltip("If robot moved less than this between checks, we treat it as 'not progressing'.")]
        public float stuckMinMove = 0.03f;
        [Tooltip("Time of no-progress before escape maneuver triggers.")]
        public float stuckTimeToTrigger = 1.1f;
        [Tooltip("Duration of escape maneuver once triggered.")]
        public float escapeDuration = 0.8f;
        [Tooltip("How much to bias escape maneuver backwards from desired direction.")]
        [Range(0f, 1f)]
        public float escapeBackoff = 0.55f;

        public LayerMask obstacleLayer;
        public LayerMask garbageLayer;

        private FuzzyFunction fuzzyFunction = new FuzzyFunction();

        public Transform frontSensor;
        public Transform frontLeftSensor;
        public Transform frontRightSensor;
        public Transform back1Sensor;
        public Transform back2Sensor;
        public Transform leftSensor;
        public Transform rightSensor;

        public bool showDebug = true;

        private Inventory inventory;
        private Rigidbody2D rb;
        private Dictionary<Types.GType, Transform> trashbins = new Dictionary<Types.GType, Transform>();
        private Vector2 movementDirection = Vector2.zero;
        private Transform currentTarget = null;
        private Types.GType currentGarbageType = Types.GType.None;
        private Vector2 searchDirection = Vector2.right;
        private float searchChangeTime = 2f;
        private float searchTimer = 0f;
        private float rotationTimer = 0f;
        private float currentRotationTime = 0f;
        private bool isAvoiding = false;
        private bool isRotating360 = false;
        private Vector2 rotationStartDirection;
        private bool isNearBoundary = false;
        private float boundaryTimer = 0f;
        private Vector2 lastBoundaryNormal = Vector2.zero;
        private Vector2 desiredVelocity = Vector2.zero;
        private Vector2 smoothedMoveDirection = Vector2.zero;
        private float targetSpeed = 0f;
        private float cachedMaxSpeed = 0f;

        // Avoidance side memory: -1 = left, +1 = right, 0 = not set
        private int avoidSide = 0;
        private float avoidSideTimer = 0f;

        // Stuck / escape
        private float stuckCheckTimer = 0f;
        private float stuckTimer = 0f;
        private Vector2 lastStuckPos = Vector2.zero;
        private bool isEscaping = false;
        private float escapeTimer = 0f;
        private Vector2 escapeDirection = Vector2.zero;

        private enum RobotState { Searching, GoingToGarbage, GoingToTrashbin, Unloading }
        private RobotState currentState = RobotState.Searching;

        void Start()
        {
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                Debug.LogError("Inventory component not found!");
                enabled = false;
                return;
            }

            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Управление движением берём на себя: без гравитации, без вращения, движение через MovePosition в FixedUpdate.
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            // Отключаем ручное управление, чтобы не конфликтовало с ИИ-движением.
            Move manualMove = GetComponent<Move>();
            if (manualMove != null)
            {
                manualMove.enabled = false;
            }

            FindAllTrashbins();
            searchDirection = Random.insideUnitCircle.normalized;
            rotationStartDirection = searchDirection;

            smoothedMoveDirection = searchDirection;
            lastStuckPos = transform.position;
            cachedMaxSpeed = (maxMoveSpeed > 0f) ? maxMoveSpeed : speed;
            targetSpeed = cachedMaxSpeed;
            speed = Mathf.Clamp(speed, 0f, cachedMaxSpeed);

            // In the current scene, sensor GameObjects have an extra Raycast debug script attached.
            // It duplicates rays and makes it look like there are "many sensors".
            DisableSensorDebug(frontSensor);
            DisableSensorDebug(frontLeftSensor);
            DisableSensorDebug(frontRightSensor);
            DisableSensorDebug(leftSensor);
            DisableSensorDebug(rightSensor);
            DisableSensorDebug(back1Sensor);
            DisableSensorDebug(back2Sensor);
        }

        void Update()
        {
            CheckBoundariesWithSensors();
            UpdateRobotState();

            if (isRotating360)
            {
                CheckForGarbageDuringRotation();
            }

            SelectTargetBasedOnState();
            CalculateMovement();
            SmoothMovementAndSpeed();
            ApplyRotation();
            if (showDebug) DebugInfo();
        }

        void FixedUpdate()
        {
            // Движение выполняем в физическом тике, чтобы корректно работали коллайдеры/стены.
            UpdateStuckDetection(Time.fixedDeltaTime);

            Vector2 moveDirForPhysics = smoothedMoveDirection;

            // Если выполняется разворот на 360°, останавливаем движение
            if (isRotating360)
            {
                moveDirForPhysics = Vector2.zero;
            }

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
                // Ограничиваем ускорение, чтобы робот не "трясся" из-за резких перепадов steering-а.
                rb.velocity = Vector2.MoveTowards(rb.velocity, desiredVelocity, maxAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                transform.position += (Vector3)(desiredVelocity * Time.fixedDeltaTime);
            }
        }

        void CheckBoundariesWithSensors()
        {
            // Вычисляем "опасную" границу по сенсорам: берём ближайшее попадание и уходим по нормали стены.
            bool wasNearBoundary = isNearBoundary;
            isNearBoundary = false;

            Vector2 travelDir = GetTravelDirectionForBoundary();

            RaycastHit2D bestHit = default;
            bool hasHit = false;
            float bestDist = float.MaxValue;
            Vector2 bestNormal = Vector2.zero;

            // Boundary detection is intentionally conservative: we only react if we're actually moving INTO a wall.
            // This prevents side sensors from constantly forcing turn-aways while sliding along walls/corridors.
            EvaluateBoundaryHit(frontSensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(frontLeftSensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(frontRightSensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(back1Sensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(back2Sensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(leftSensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(rightSensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);

            if (hasHit)
            {
                isNearBoundary = true;
                boundaryTimer += Time.deltaTime;

                Vector2 boundaryNormal = bestNormal.sqrMagnitude > 0.0001f ? bestNormal.normalized : Vector2.zero;

                if (!wasNearBoundary ||
                    Vector2.Dot(boundaryNormal, lastBoundaryNormal) < 0.7f ||
                    boundaryTimer >= boundaryCooldown)
                {
                    // Разворачиваемся ОТ стены по нормали
                    searchDirection = boundaryNormal.sqrMagnitude > 0.0001f ? boundaryNormal : Random.insideUnitCircle.normalized;

                    // Добавляем случайный угол от -45 до 45 градусов
                    float randomAngle = Random.Range(-45f, 45f);
                    searchDirection = Quaternion.Euler(0, 0, randomAngle) * searchDirection;

                    searchTimer = 0f;
                    boundaryTimer = 0f;
                    lastBoundaryNormal = boundaryNormal;

                    Debug.Log($"Near boundary! Turning away to: {searchDirection}");
                }
            }
            else
            {
                lastBoundaryNormal = Vector2.zero;
                boundaryTimer = 0f;
            }
        }

        void EvaluateBoundaryHit(Transform sensor, Vector2 travelDir, ref bool hasHit, ref float bestDist, ref RaycastHit2D bestHit, ref Vector2 bestNormal)
        {
            if (sensor == null) return;
            Vector2 dir = GetSensorWorldDirection(sensor);
            if (dir.sqrMagnitude < 0.0001f) return;

            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, safeZoneDistance, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(sensor.position, dir * safeZoneDistance, hit.collider ? Color.magenta : Color.cyan);
            }

            if (hit.collider == null) return;

            // React only if we're approaching the wall, not sliding parallel to it.
            if (travelDir.sqrMagnitude > 0.0001f && hit.normal.sqrMagnitude > 0.0001f)
            {
                float approach = Vector2.Dot(travelDir.normalized, hit.normal.normalized);
                if (approach > -Mathf.Max(0.001f, boundaryApproachThreshold))
                {
                    return;
                }
            }

            float d = hit.distance;
            if (d < bestDist)
            {
                hasHit = true;
                bestDist = d;
                bestHit = hit;
                bestNormal = hit.normal;
            }
        }

        void FindAllTrashbins()
        {
            TrashBin[] bins = FindObjectsOfType<TrashBin>();
            foreach (var bin in bins)
            {
                if (!trashbins.ContainsKey(bin.garbageType))
                    trashbins[bin.garbageType] = bin.transform;
            }
        }

        void UpdateRobotState()
        {
            Types.GType inventoryGarbage = inventory.getCell();

            if (inventoryGarbage != currentGarbageType)
            {
                currentGarbageType = inventoryGarbage;

                if (inventoryGarbage == Types.GType.None)
                {
                    currentState = RobotState.Searching;
                    currentTarget = null;
                }
                else
                {
                    currentState = RobotState.GoingToTrashbin;
                }
            }

            if (currentState == RobotState.GoingToTrashbin && currentTarget != null)
            {
                float distance = Vector2.Distance(transform.position, currentTarget.position);
                if (distance < 0.5f)
                {
                    currentState = RobotState.Unloading;
                }
            }
        }

        void SelectTargetBasedOnState()
        {
            switch (currentState)
            {
                case RobotState.Searching:
                    if (!isRotating360)
                    {
                        FindGarbageWithRay();
                    }
                    if (currentTarget != null)
                    {
                        currentState = RobotState.GoingToGarbage;
                    }
                    break;

                case RobotState.GoingToGarbage:
                    if (inventory.getCell() != Types.GType.None)
                    {
                        currentState = RobotState.GoingToTrashbin;
                        currentTarget = null;
                    }
                    break;

                case RobotState.GoingToTrashbin:
                    if (currentTarget == null && trashbins.ContainsKey(currentGarbageType))
                    {
                        currentTarget = trashbins[currentGarbageType];
                    }
                    break;
            }
        }

        void FindGarbageWithRay()
        {
            currentTarget = null;

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                searchDirection,
                detectionRadius,
                garbageLayer
            );

            if (hit.collider != null)
            {
                currentTarget = hit.collider.transform;
                Debug.Log($"Found garbage with ray! Distance: {hit.distance:F1}");
            }
        }

        void CheckForGarbageDuringRotation()
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                searchDirection,
                detectionRadius,
                garbageLayer
            );

            if (hit.collider != null)
            {
                currentTarget = hit.collider.transform;
                isRotating360 = false;
                currentState = RobotState.GoingToGarbage;
                Debug.Log($"Detected garbage during rotation at {hit.distance:F1} meters!");
            }
        }

        void CalculateMovement()
        {
            if (isRotating360)
            {
                Handle360Rotation();
                movementDirection = Vector2.zero;
                return;
            }

            rotationTimer += Time.deltaTime;

            // Разворот на 360° - только в состоянии Searching
            if (rotationTimer >= rotationInterval &&
                currentState == RobotState.Searching &&
                !isNearBoundary)
            {
                // Но сначала проверим, есть ли мусор в поле зрения
                bool canStartRotation = true;

                // Проверим сенсорами, нет ли мусора впереди
                if (frontSensor != null)
                {
                    RaycastHit2D hit = Physics2D.Raycast(
                        frontSensor.position,
                        GetSensorWorldDirection(frontSensor),
                        detectionRadius,
                        garbageLayer
                    );
                    if (hit.collider != null)
                    {
                        // Если видим мусор - не разворачиваемся, идем к нему
                        currentTarget = hit.collider.transform;
                        currentState = RobotState.GoingToGarbage;
                        rotationTimer = 0f; // Сбросим таймер
                        canStartRotation = false;
                        Debug.Log($"Found garbage during rotation check! Going to: {hit.collider.name}");
                    }
                }

                if (canStartRotation)
                {
                    Start360Rotation();
                    return;
                }
            }

            Vector2 targetDirection = Vector2.zero;

            if (currentTarget != null && currentState != RobotState.Unloading)
            {
                Vector2 toTarget = (Vector2)currentTarget.position - (Vector2)transform.position;
                float distance = toTarget.magnitude;
                if (distance < 0.3f)
                {
                    movementDirection = Vector2.zero;
                    return;
                }

                // Safe zone проверка при движении к цели
                if (isNearBoundary)
                {
                    Debug.Log("Too close to boundary - moving away before target!");
                    movementDirection = searchDirection;
                    isAvoiding = true;
                    return;
                }

                // Если застряли — делаем короткий "escape" манёвр
                if (isEscaping)
                {
                    movementDirection = escapeDirection;
                    isAvoiding = true;
                    return;
                }

                // Более стабильное уклонение
                movementDirection = ComputeFuzzySteeredDirection(toTarget.normalized);

                // Speed from sensors
                float frontDist = GetMinForwardDistanceForSpeed();
                targetSpeed = ComputeFuzzySpeedFromSensors(movementDirection);

                // Extra safety: slow down on sharp turns
                ApplyTurnSpeedLimit(frontDist, movementDirection);
                isAvoiding = IsObstacleVeryClose();

                return;
            }
            else if (currentState == RobotState.Searching)
            {
                // Если близко к границе - двигаемся в безопасном направлении
                if (isNearBoundary)
                {
                    movementDirection = searchDirection;
                    return;
                }

                // Обычный поисковой патруль
                searchTimer += Time.deltaTime;
                if (searchTimer > searchChangeTime)
                {
                    searchDirection = Random.insideUnitCircle.normalized;
                    searchTimer = 0f;
                    searchChangeTime = Random.Range(1f, 3f);
                }
                targetDirection = searchDirection;
            }
            else if (currentState == RobotState.Unloading)
            {
                movementDirection = Vector2.zero;
                return;
            }

            if (isEscaping)
            {
                movementDirection = escapeDirection;
                isAvoiding = true;
                rotationTimer = 0f;
                return;
            }

            // Для состояния Searching (когда нет цели) используем вычисленное направление
            movementDirection = ComputeFuzzySteeredDirection(targetDirection.normalized);

            // Speed from sensors
            float frontDistSearching = GetMinForwardDistanceForSpeed();
            targetSpeed = ComputeFuzzySpeedFromSensors(movementDirection);

            // Turn-slow for searching movement as well
            ApplyTurnSpeedLimit(frontDistSearching, movementDirection);
            isAvoiding = IsObstacleVeryClose();
            if (isAvoiding) rotationTimer = 0f;
        }

        float ComputeFuzzySpeedFromSensors(Vector2 moveDir)
        {
            // Fuzzy speed is computed per sensor (center-of-mass method) and aggregated using fuzzy AND (min),
            // so ANY close obstacle forces speed down.

            float rangeForward = Mathf.Max(1f, obstacleAvoidDistance * 2f);
            float rangeSide = Mathf.Max(1f, Mathf.Max(safeDistance * 2f, obstacleAvoidDistance));

            float dFront = CheckObstacleDistanceInRange(frontSensor, rangeForward, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));
            float dFL = CheckObstacleDistanceInRange(frontLeftSensor, rangeForward, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));
            float dFR = CheckObstacleDistanceInRange(frontRightSensor, rangeForward, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));
            float dL = CheckObstacleDistanceInRange(leftSensor, rangeSide, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));
            float dR = CheckObstacleDistanceInRange(rightSensor, rangeSide, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));
            float dBL = CheckObstacleDistanceInRange(back1Sensor, rangeSide, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));
            float dBR = CheckObstacleDistanceInRange(back2Sensor, rangeSide, new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f));

            float dMove = GetClearanceDistanceForSpeed(moveDir);

            float sFront = fuzzyFunction.Sentr_mass(dFront);
            float sFL = fuzzyFunction.Sentr_mass(dFL);
            float sFR = fuzzyFunction.Sentr_mass(dFR);
            float sMove = fuzzyFunction.Sentr_mass(dMove == float.MaxValue ? rangeForward : dMove);

            // Side / back have smaller influence on forward speed (still can slow down in corridors).
            float sL = fuzzyFunction.Sentr_mass(dL) * 0.9f;
            float sR = fuzzyFunction.Sentr_mass(dR) * 0.9f;
            float sBL = fuzzyFunction.Sentr_mass(dBL) * 0.8f;
            float sBR = fuzzyFunction.Sentr_mass(dBR) * 0.8f;

            float s = Mathf.Min(sFront, sFL, sFR, sMove, sL, sR, sBL, sBR);

            float maxSpd = (maxMoveSpeed > 0f) ? maxMoveSpeed : cachedMaxSpeed;
            if (maxSpd > 0f) s = Mathf.Min(s, maxSpd);
            s = Mathf.Max(0f, s);
            return s;
        }

        Vector2 ComputeFuzzySteeredDirection(Vector2 desiredDir)
        {
            if (desiredDir.sqrMagnitude < 0.0001f) return Vector2.zero;

            // Distances for fuzzy steering (closer => higher membership in "near obstacle").
            float dFront = CheckObstacleDistanceInRange(frontSensor, obstacleAvoidDistance, Color.red, Color.green);
            float dFL = CheckObstacleDistanceInRange(frontLeftSensor, obstacleAvoidDistance, Color.red, Color.green);
            float dFR = CheckObstacleDistanceInRange(frontRightSensor, obstacleAvoidDistance, Color.red, Color.green);
            float dL = CheckObstacleDistanceInRange(leftSensor, safeDistance, Color.red, Color.green);
            float dR = CheckObstacleDistanceInRange(rightSensor, safeDistance, Color.red, Color.green);
            float dBL = CheckObstacleDistanceInRange(back1Sensor, safeDistance, Color.red, Color.green);
            float dBR = CheckObstacleDistanceInRange(back2Sensor, safeDistance, Color.red, Color.green);

            // Membership degrees (0 far .. 1 very close).
            float nearFront = weightFront * FuzzyNear01(dFront, obstacleAvoidDistance);
            float nearFL = weightFrontDiag * FuzzyNear01(dFL, obstacleAvoidDistance);
            float nearFR = weightFrontDiag * FuzzyNear01(dFR, obstacleAvoidDistance);
            float nearL = weightSide * FuzzyNear01(dL, safeDistance);
            float nearR = weightSide * FuzzyNear01(dR, safeDistance);
            float nearBL = weightBack * FuzzyNear01(dBL, safeDistance);
            float nearBR = weightBack * FuzzyNear01(dBR, safeDistance);

            // Aggregate "obstacle on side" degrees.
            float obstacleLeft = Mathf.Clamp01(Mathf.Max(nearL, nearFL, nearBL));
            float obstacleRight = Mathf.Clamp01(Mathf.Max(nearR, nearFR, nearBR));
            float obstacleFront = Mathf.Clamp01(Mathf.Max(nearFront, 0.6f * Mathf.Max(nearFL, nearFR)));

            // Simple Sugeno-like fuzzy inference with 5 outputs: hardLeft, left, straight, right, hardRight.
            // Positive output = turn LEFT (CCW), negative = turn RIGHT (CW).
            float wHardLeft = Mathf.Min(obstacleFront, obstacleRight);
            float wHardRight = Mathf.Min(obstacleFront, obstacleLeft);
            float wLeft = obstacleRight * (1f - obstacleLeft);
            float wRight = obstacleLeft * (1f - obstacleRight);
            float wStraight = (1f - Mathf.Max(obstacleLeft, obstacleRight)) * (1f - 0.6f * obstacleFront);

            // If both sides are "near", bias to the more open side (continuous output).
            float wBias = Mathf.Min(obstacleLeft, obstacleRight);
            float openLeft = 1f - obstacleLeft;
            float openRight = 1f - obstacleRight;
            float biasOut = Mathf.Clamp(openLeft - openRight, -1f, 1f); // + => left more open => turn left

            float num =
                wHardLeft * 1f +
                wLeft * 0.5f +
                wStraight * 0f +
                wRight * -0.5f +
                wHardRight * -1f +
                wBias * biasOut;

            float den = wHardLeft + wLeft + wStraight + wRight + wHardRight + wBias;
            float steer01 = (den > 0.0001f) ? (num / den) : 0f;
            steer01 = Mathf.Clamp(steer01 * steerGain, -1f, 1f);

            float angle = steer01 * maxSteerAngleDeg;
            Vector2 result = (Vector2)(Quaternion.Euler(0f, 0f, angle) * desiredDir.normalized);
            return result.sqrMagnitude > 0.0001f ? result.normalized : desiredDir.normalized;
        }

        float CheckObstacleDistanceInRange(Transform sensor, float range, Color hitColor, Color missColor)
        {
            if (sensor == null) return float.MaxValue;
            Vector2 dir = GetSensorWorldDirection(sensor);
            if (dir.sqrMagnitude < 0.0001f) return float.MaxValue;

            float len = Mathf.Max(0.01f, range);
            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, len, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(sensor.position, dir * len, hit.collider ? hitColor : missColor);
            }
            return hit.collider ? Mathf.Max(0.0001f, hit.distance) : float.MaxValue;
        }

        float FuzzyNear01(float distance, float range)
        {
            // Linear membership for "Near": 1 at distance==0, 0 at distance>=range.
            float r = Mathf.Max(0.01f, range);
            if (distance == float.MaxValue) return 0f;
            return Mathf.Clamp01(1f - (Mathf.Clamp(distance, 0f, r) / r));
        }

        void SmoothMovementAndSpeed()
        {
            // Если выполняется разворот на 360°, сглаживаем движение к нулю
            if (isRotating360)
            {
                float dtа = Time.deltaTime;
                float dirAlphaa = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * dtа);
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, Vector2.zero, dirAlphaa);
                return;
            }

            // Экспоненциальное сглаживание (не зависит от FPS).
            float dt = Time.deltaTime;
            float dirAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * dt);
            float spdAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, speedSmoothing) * dt);

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                // Emergency: if obstacle is extremely close in front, don't lag steering (prevents late turns -> collisions).
                float frontDist = GetMinForwardDistanceForSpeed();
                if (frontDist <= emergencySnapDistance)
                {
                    smoothedMoveDirection = movementDirection.normalized;
                }
                else
                {
                    smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, movementDirection.normalized, dirAlpha);
                }
            }
            else
            {
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, Vector2.zero, dirAlpha);
            }

            speed = Mathf.Lerp(speed, targetSpeed, spdAlpha);
            speed = Mathf.Max(0f, speed);
            if (maxMoveSpeed > 0f) speed = Mathf.Min(speed, maxMoveSpeed);
            else if (cachedMaxSpeed > 0f) speed = Mathf.Min(speed, cachedMaxSpeed);

            // Таймер фиксации стороны объезда
            if (avoidSideTimer > 0f) avoidSideTimer -= dt;
        }

        void ApplyTurnSpeedLimit(float frontDist, Vector2 desiredMoveDir)
        {
            if (rb == null) return;
            if (targetSpeed <= 0.01f) return;
            if (desiredMoveDir.sqrMagnitude < 0.0001f) return;

            Vector2 currentVel = rb.velocity;
            if (currentVel.sqrMagnitude < 0.01f) return;

            float angle = Vector2.Angle(currentVel.normalized, desiredMoveDir.normalized);
            float t = Mathf.InverseLerp(Mathf.Max(0f, turnSlowStartAngle), Mathf.Max(turnSlowStartAngle + 0.01f, turnSlowFullAngle), angle);
            float factor = Mathf.Lerp(1f, sharpTurnSpeedFactor, t);

            // When something is very close in front, clamp even harder (regardless of fuzzy output).
            if (frontDist <= emergencySnapDistance)
            {
                factor = Mathf.Min(factor, sharpTurnSpeedFactor);
            }

            targetSpeed *= Mathf.Clamp01(factor);
        }

        Vector2 GetTravelDirectionForBoundary()
        {
            if (rb != null && rb.velocity.sqrMagnitude > 0.01f) return rb.velocity;
            if (smoothedMoveDirection.sqrMagnitude > 0.01f) return smoothedMoveDirection;
            if (movementDirection.sqrMagnitude > 0.01f) return movementDirection;
            if (searchDirection.sqrMagnitude > 0.01f) return searchDirection;
            return Vector2.zero;
        }

        void DisableSensorDebug(Transform sensor)
        {
            if (sensor == null) return;
            // Raycast2DExample is only for visual debugging; it duplicates rays and confuses tuning.
            Raycast2DExample dbg = sensor.GetComponent<Raycast2DExample>();
            if (dbg != null) dbg.enabled = false;
        }

        Vector2 ComputeSteeredDirection(Vector2 desiredDir)
        {
            if (desiredDir.sqrMagnitude < 0.0001f) return Vector2.zero;

            // Собираем информацию по препятствиям
            float clearDistance = obstacleAvoidDistance * Mathf.Max(1f, clearFactor);
            bool hasObstacle = false;
            Vector2 repulse = Vector2.zero;
            Vector2 tangent = Vector2.zero;

            // Репульсия и касательная составляющая по нормалям попаданий.
            AddAvoidanceAndTangent(frontSensor, obstacleAvoidDistance, ref repulse, ref tangent, ref hasObstacle);
            AddAvoidanceAndTangent(frontLeftSensor, obstacleAvoidDistance, ref repulse, ref tangent, ref hasObstacle);
            AddAvoidanceAndTangent(frontRightSensor, obstacleAvoidDistance, ref repulse, ref tangent, ref hasObstacle);
            AddAvoidanceAndTangent(leftSensor, safeDistance, ref repulse, ref tangent, ref hasObstacle);
            AddAvoidanceAndTangent(rightSensor, safeDistance, ref repulse, ref tangent, ref hasObstacle);
            AddAvoidanceAndTangent(back1Sensor, safeDistance, ref repulse, ref tangent, ref hasObstacle);
            AddAvoidanceAndTangent(back2Sensor, safeDistance, ref repulse, ref tangent, ref hasObstacle);

            // Если препятствий рядом нет — отпускаем "память" стороны.
            if (!hasObstacle)
            {
                // Доп. проверка "чисто ли впереди": если всё далеко — сбрасываем сторону.
                if (frontSensor == null || CheckObstacleDistance(frontSensor) > clearDistance)
                {
                    avoidSide = 0;
                    avoidSideTimer = 0f;
                }
                return desiredDir.normalized;
            }

            // Выбираем сторону объезда (и держим некоторое время, чтобы не дёргаться).
            if (avoidSide == 0 || avoidSideTimer <= 0f)
            {
                avoidSide = ChooseAvoidSide(desiredDir);
                if (avoidSide == 0) avoidSide = Random.value < 0.5f ? -1 : 1;
                avoidSideTimer = avoidSideLockTime;
            }

            Vector2 steer = desiredDir.normalized;
            Vector2 avoidance = repulse * avoidanceWeight;
            Vector2 along = tangent.sqrMagnitude > 0.0001f ? tangent.normalized * (float)avoidSide : Vector2.zero;
            along *= tangentWeight * Mathf.Clamp01(repulse.magnitude + 0.25f);

            Vector2 combined = steer + avoidance + along;
            if (combined.sqrMagnitude < 0.0001f)
            {
                // Если вектор почти нулевой (случается в "коридоре") — толкаем в сторону объезда.
                Vector2 perp = Vector2.Perpendicular(desiredDir).normalized;
                combined = perp * avoidSide;
            }
            return combined.normalized;
        }

        void AddAvoidanceAndTangent(Transform sensor, float thresholdDistance, ref Vector2 repulse, ref Vector2 tangent, ref bool hasObstacle)
        {
            if (sensor == null) return;
            Vector2 dir = GetSensorWorldDirection(sensor);
            if (dir.sqrMagnitude < 0.0001f) return;

            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, thresholdDistance, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(sensor.position, dir * thresholdDistance, hit.collider ? Color.red : Color.green);
            }
            if (hit.collider == null) return;

            hasObstacle = true;
            float dist = Mathf.Max(hit.distance, 0.0001f);
            float w = Mathf.Clamp01((thresholdDistance - dist) / thresholdDistance);

            Vector2 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : -dir;
            repulse += normal * w;

            // Касательная к поверхности: перпендикуляр к нормали.
            Vector2 tan = Vector2.Perpendicular(normal);
            if (tan.sqrMagnitude > 0.0001f)
            {
                tangent += tan.normalized * w;
            }
        }

        int ChooseAvoidSide(Vector2 desiredDir)
        {
            // Простая эвристика: выбираем сторону, где "свободнее" (по левому/правому сенсорам).
            float leftSide = leftSensor ? CheckObstacleDistance(leftSensor) : float.MaxValue;
            float rightSide = rightSensor ? CheckObstacleDistance(rightSensor) : float.MaxValue;
            float leftFront = frontLeftSensor ? CheckObstacleDistance(frontLeftSensor) : float.MaxValue;
            float rightFront = frontRightSensor ? CheckObstacleDistance(frontRightSensor) : float.MaxValue;

            // Для выбора стороны учитываем и "габарит" вперёд по диагонали.
            float left = Mathf.Min(leftSide, leftFront);
            float right = Mathf.Min(rightSide, rightFront);

            if (Mathf.Abs(left - right) < 0.05f)
            {
                // Если одинаково — выбираем по знаку векторного произведения (куда "естественнее" повернуть).
                Vector2 perp = Vector2.Perpendicular(desiredDir);
                // Если perp ближе к направлению tangent суммарно, можно было бы учитывать; оставим 0.
                return 0;
            }
            // Больше дистанция => туда и объезжаем: left bigger => поворачиваем влево (-1), right bigger => вправо (+1)
            return (right > left) ? 1 : -1;
        }

        Vector2 GetSensorWorldDirection(Transform sensor)
        {
            // Сенсор — точка на корпусе. Направление "наружу" берём от центра робота к сенсору.
            Vector2 fromCenter = (Vector2)sensor.position - (Vector2)transform.position;
            if (fromCenter.sqrMagnitude < 0.0001f) return Vector2.zero;
            return fromCenter.normalized;
        }

        void Start360Rotation()
        {
            isRotating360 = true;
            currentRotationTime = 0f;
            rotationStartDirection = searchDirection;
            isAvoiding = false;

            // При начале разворота сразу останавливаем движение
            movementDirection = Vector2.zero;
            smoothedMoveDirection = Vector2.zero;

            Debug.Log("Starting 360-degree rotation on spot");
        }

        void Handle360Rotation()
        {
            currentRotationTime += Time.deltaTime;

            if (currentRotationTime >= rotationDuration)
            {
                isRotating360 = false;
                rotationTimer = 0f;
                searchDirection = Quaternion.Euler(0, 0, 360f) * rotationStartDirection;
                Debug.Log("360-degree rotation on spot completed");
                return;
            }

            float progress = currentRotationTime / rotationDuration;
            float rotationAngle = 360f * progress;

            searchDirection = Quaternion.Euler(0, 0, rotationAngle) * rotationStartDirection;
            // Не устанавливаем movementDirection - робот стоит на месте
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

        void ApplyRotation()
        {
            // При развороте на 360° поворачиваемся по searchDirection
            if (isRotating360 && searchDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(searchDirection.y, searchDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
            else if (smoothedMoveDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(smoothedMoveDirection.y, smoothedMoveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        void UpdateStuckDetection(float dt)
        {
            // Не проверяем застревание во время разворота на 360°
            if (isRotating360)
            {
                ResetStuck();
                return;
            }

            if (currentState == RobotState.Unloading)
            {
                ResetStuck();
                return;
            }

            // Если нечётная логика "просит остановиться" — обычно это не застревание.
            // Но если рядом реально препятствие/граница, робот может "залипнуть" в ноль скорости и не сделать escape.
            if (speed < 0.15f || targetSpeed < 0.15f)
            {
                if (!IsObstacleVeryClose())
                {
                    stuckTimer = 0f;
                    return;
                }
            }

            // Escape таймер
            if (isEscaping)
            {
                escapeTimer -= dt;
                if (escapeTimer <= 0f)
                {
                    isEscaping = false;
                    escapeDirection = Vector2.zero;
                    // После escape чуть держим сторону, чтобы не "влипнуть" обратно.
                    if (avoidSide == 0) avoidSide = Random.value < 0.5f ? -1 : 1;
                    avoidSideTimer = Mathf.Max(avoidSideTimer, avoidSideLockTime * 0.5f);
                }
            }

            // Проверка прогресса раз в интервал
            stuckCheckTimer += dt;
            if (stuckCheckTimer < stuckCheckInterval) return;
            stuckCheckTimer = 0f;

            float moved = Vector2.Distance(transform.position, lastStuckPos);
            lastStuckPos = transform.position;

            // Если мы вообще не пытаемся ехать — не считаем застреванием.
            if (desiredVelocity.magnitude < 0.2f && movementDirection.magnitude < 0.2f)
            {
                stuckTimer = 0f;
                return;
            }

            if (moved < stuckMinMove)
            {
                stuckTimer += stuckCheckInterval;
            }
            else
            {
                stuckTimer = 0f;
            }

            if (!isEscaping && stuckTimer >= stuckTimeToTrigger)
            {
                // Escape запускаем только если реально есть препятствие рядом (иначе можно "сорваться" на ровном месте).
                float minDist = float.MaxValue;
                if (frontSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontSensor));
                if (frontLeftSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontLeftSensor));
                if (frontRightSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontRightSensor));
                if (leftSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(leftSensor));
                if (rightSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(rightSensor));
                if (back1Sensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(back1Sensor));
                if (back2Sensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(back2Sensor));

                bool nearObstacle = minDist < obstacleAvoidDistance * 1.1f || isNearBoundary;
                if (nearObstacle)
                {
                    StartEscapeManeuver();
                }
                else
                {
                    // Нет препятствий рядом — сбрасываем таймер, чтобы не дёргаться.
                    stuckTimer = 0f;
                }
            }
        }

        float GetMinForwardDistanceForSpeed()
        {
            // Для скорости берём "самое близкое вперёд": центр + два диагональных габарита.
            float d = float.MaxValue;
            if (frontSensor) d = Mathf.Min(d, CheckObstacleDistance(frontSensor));
            if (frontLeftSensor) d = Mathf.Min(d, CheckObstacleDistance(frontLeftSensor));
            if (frontRightSensor) d = Mathf.Min(d, CheckObstacleDistance(frontRightSensor));
            return d;
        }

        float GetClearanceDistanceForSpeed(Vector2 moveDir)
        {
            if (moveDir.sqrMagnitude < 0.0001f) return float.MaxValue;
            Vector2 dir = moveDir.normalized;
            float len = Mathf.Max(obstacleAvoidDistance, safeDistance) * 2f;

            // Small forward offset helps when the robot is touching colliders.
            Vector2 origin = (Vector2)transform.position + dir * 0.02f;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, len, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(origin, dir * len, hit.collider ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.3f));
            }
            return hit.collider ? hit.distance : float.MaxValue;
        }

        bool IsObstacleVeryClose()
        {
            float minDist = float.MaxValue;
            if (frontSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontSensor));
            if (frontLeftSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontLeftSensor));
            if (frontRightSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontRightSensor));
            if (leftSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(leftSensor));
            if (rightSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(rightSensor));
            if (back1Sensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(back1Sensor));
            if (back2Sensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(back2Sensor));

            return isNearBoundary || minDist < obstacleAvoidDistance * 0.8f;
        }

        void StartEscapeManeuver()
        {
            stuckTimer = 0f;
            isEscaping = true;
            escapeTimer = escapeDuration;

            Vector2 baseDir = movementDirection.sqrMagnitude > 0.0001f ? movementDirection.normalized : searchDirection.normalized;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.right;

            if (avoidSide == 0) avoidSide = Random.value < 0.5f ? -1 : 1;

            Vector2 side = Vector2.Perpendicular(baseDir).normalized * avoidSide;
            Vector2 back = (-baseDir) * escapeBackoff;
            Vector2 combined = side + back;

            if (combined.sqrMagnitude < 0.0001f) combined = side;
            escapeDirection = combined.normalized;

            // На время escape фиксируем сторону, чтобы не дёргаться.
            avoidSideTimer = Mathf.Max(avoidSideTimer, avoidSideLockTime);
        }

        void ResetStuck()
        {
            stuckTimer = 0f;
            stuckCheckTimer = 0f;
            isEscaping = false;
            escapeTimer = 0f;
            escapeDirection = Vector2.zero;
            lastStuckPos = transform.position;
        }

        void DebugInfo()
        {
            string stateStr = "";
            switch (currentState)
            {
                case RobotState.Searching: stateStr = "Searching"; break;
                case RobotState.GoingToGarbage: stateStr = "GoingToGarbage"; break;
                case RobotState.GoingToTrashbin: stateStr = "GoingToTrashbin"; break;
                case RobotState.Unloading: stateStr = "Unloading"; break;
            }

            string targetStr = currentTarget != null ?
                (currentState == RobotState.GoingToTrashbin ? "Trashbin" : "Garbage") : "No target";

            string rotationStr = isRotating360 ? $"Rotating 360° ({(currentRotationTime / rotationDuration) * 100:F0}%)" : "Not rotating";

            string info = $"State: {stateStr} | Garbage: {currentGarbageType} | Target: {targetStr} | Avoiding: {isAvoiding} (side={avoidSide}) | Escaping: {isEscaping} | NearBoundary: {isNearBoundary} | {rotationStr} | Speed: {speed:F2}";
            Debug.Log(info);
        }

        void OnDrawGizmos()
        {
            if (!showDebug) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, smoothedMoveDirection * 1f);

            if (currentTarget != null)
            {
                Gizmos.color = (currentState == RobotState.GoingToTrashbin) ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
                Gizmos.DrawWireSphere(currentTarget.position, 0.3f);
            }

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, obstacleAvoidDistance);

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, safeDistance);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, safeZoneDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, searchDirection * detectionRadius);
        }
    }
}