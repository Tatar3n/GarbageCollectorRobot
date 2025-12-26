using UnityEngine;
using System.Collections.Generic;

namespace Fuzzy
{
    public class FuzzyLogicSystem : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 3f;
        public float detectionRadius = 5f;
        public float obstacleAvoidDistance = 1.5f;
        public float rotationInterval = 5f;
        public float safeDistance = 0.3f;
        public float rotationDuration = 1f;
        public float safeZoneDistance = 2f;
        public float boundaryCooldown = 1f;

        [Header("Steering / Anti-jitter")]
        [Tooltip("How fast direction changes are smoothed. Higher = snappier, lower = smoother.")]
        public float directionSmoothing = 10f;
        [Tooltip("How fast speed changes are smoothed (for fuzzy output).")]
        public float speedSmoothing = 8f;
        [Tooltip("Max acceleration applied to Rigidbody2D velocity (reduces shaking).")]
        public float maxAcceleration = 20f;

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

        private readonly FuzzyFunction fuzzyFunction = new FuzzyFunction();

        public Transform frontSensor;
        public Transform back1Sensor;
        public Transform back2Sensor;

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
        private float lastGabaritMinDist = float.MaxValue;
        private float lastGabaritTurnAngle = 0f;

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
            targetSpeed = speed;

            // In the current scene, sensor GameObjects have an extra Raycast debug script attached.
            // It duplicates rays and makes it look like there are "many sensors".
            DisableSensorDebug(frontSensor);
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
            EvaluateBoundaryHit(back1Sensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(back2Sensor, travelDir, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);

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

                Vector2 baseDir = toTarget.normalized;
                movementDirection = ApplyFuzzyObstacleTurn(baseDir);

                // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ (по переднему датчику)
                float frontDist = CheckObstacleDistance(frontSensor);
                float speedDist = Mathf.Min(frontDist, lastGabaritMinDist);
                targetSpeed = fuzzyFunction.Sentr_mass(speedDist);
                ApplyTurnSpeedLimit(frontDist, movementDirection);
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
                Vector2 baseDir = searchDirection.sqrMagnitude > 0.0001f ? searchDirection.normalized : Vector2.right;
                movementDirection = ApplyFuzzyObstacleTurn(baseDir);

                float frontDist = CheckObstacleDistance(frontSensor);
                float speedDist = Mathf.Min(frontDist, lastGabaritMinDist);
                targetSpeed = fuzzyFunction.Sentr_mass(speedDist);
                ApplyTurnSpeedLimit(frontDist, movementDirection);
                return;
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
        }

        void SmoothMovementAndSpeed()
        {
            // Если выполняется разворот на 360°, сглаживаем движение к нулю
            if (isRotating360)
            {
                float dtA = Time.deltaTime;
                float dirAlphaA = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * dtA);
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, Vector2.zero, dirAlphaA);
                return;
            }

            // Экспоненциальное сглаживание (не зависит от FPS).
            float dt = Time.deltaTime;
            float dirAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * dt);
            float spdAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, speedSmoothing) * dt);

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                // Emergency: if obstacle is extremely close in front, don't lag steering (prevents late turns -> collisions).
                float frontDist = CheckObstacleDistance(frontSensor);
                float snapDist = Mathf.Min(frontDist, lastGabaritMinDist);
                if (snapDist <= emergencySnapDistance || isAvoiding)
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

        Vector2 ApplyFuzzyObstacleTurn(Vector2 baseDir)
        {
            if (baseDir.sqrMagnitude < 0.0001f) return Vector2.zero;

            // Берём два габаритных датчика (левый/правый). В Sentr_mass_rotate передаём расстояние
            // с датчика, который ближе к препятствию.
            float dRight = back1Sensor ? CheckObstacleDistance(back1Sensor) : float.MaxValue;
            float dLeft = back2Sensor ? CheckObstacleDistance(back2Sensor) : float.MaxValue;

            float dMin = Mathf.Min(dLeft, dRight);
            bool isLeft = dLeft <= dRight;

            float turnAngle = fuzzyFunction.Sentr_mass_rotate(dMin, isLeft);
            lastGabaritMinDist = dMin;
            lastGabaritTurnAngle = turnAngle;
            if (Mathf.Abs(turnAngle) <= 0.001f)
            {
                isAvoiding = false;
                return baseDir.normalized;
            }

            isAvoiding = true;
            rotationTimer = 0f;

            // Важно: поворачиваем ОТ текущей "базовой" цели/поиска, а не от сглаженного прошлого вектора,
            // иначе создаётся ощущение "поворота в последний момент".
            Vector2 turned = (Vector2)(Quaternion.Euler(0f, 0f, turnAngle) * baseDir.normalized);
            if (turned.sqrMagnitude < 0.0001f) return baseDir.normalized;

            return turned.normalized;
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

        bool IsObstacleVeryClose()
        {
            float minDist = float.MaxValue;
            if (frontSensor) minDist = Mathf.Min(minDist, CheckObstacleDistance(frontSensor));
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

            int avoidSide = Random.value < 0.5f ? -1 : 1;
            Vector2 side = Vector2.Perpendicular(baseDir).normalized * avoidSide;
            Vector2 back = (-baseDir) * escapeBackoff;
            Vector2 combined = side + back;

            if (combined.sqrMagnitude < 0.0001f) combined = side;
            escapeDirection = combined.normalized;
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

            string info = $"State: {stateStr} | Garbage: {currentGarbageType} | Target: {targetStr} | Avoiding: {isAvoiding} | Escaping: {isEscaping} | NearBoundary: {isNearBoundary} | {rotationStr} | Speed: {speed:F2}";
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