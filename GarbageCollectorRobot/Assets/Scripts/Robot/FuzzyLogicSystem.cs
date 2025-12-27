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
        public float rotationDuration = 1f;

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
        // Optional sensors (in SampleScene these exist as frontLeft/frontRight).
        // Used as left/right "edge" ray origins for obstacle avoidance.
        public Transform frontLeftSensor;
        public Transform frontRightSensor;
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
        private Vector2 desiredVelocity = Vector2.zero;
        private float targetSpeed = 0f;
        private float lastGabaritMinDist = float.MaxValue;
        private Vector2 lastBaseDirection = Vector2.right;

        // Stuck / escape
        private float stuckCheckTimer = 0f;
        private float stuckTimer = 0f;
        private Vector2 lastStuckPos = Vector2.zero;
        private bool isEscaping = false;
        private float escapeTimer = 0f;
        private Vector2 escapeDirection = Vector2.zero;

        private enum RobotState { Searching, GoingToGarbage, GoingToTrashbin, Unloading }
        private RobotState currentState = RobotState.Searching;

        void Awake()
        {
            // Важно делать это в Awake, чтобы никакие другие скрипты (например Move)
            // не успели начать управлять Rigidbody2D в первый физический тик.
            inventory = GetComponent<Inventory>();

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
        }

        void Start()
        {
            if (inventory == null)
            {
                Debug.LogError("Inventory component not found!");
                enabled = false;
                return;
            }

            FindAllTrashbins();
            searchDirection = Random.insideUnitCircle.normalized;
            rotationStartDirection = searchDirection;

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
            UpdateRobotState();

            if (isRotating360)
            {
                CheckForGarbageDuringRotation();
            }

            SelectTargetBasedOnState();
            if (showDebug) DebugInfo();
        }

        void FixedUpdate()
        {
            // Движение выполняем в физическом тике, чтобы корректно работали коллайдеры/стены.
            UpdateStuckDetection(Time.fixedDeltaTime);
            CalculateMovement(Time.fixedDeltaTime);
            ApplyRotation();

            Vector2 moveDirForPhysics = movementDirection;

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
                rb.velocity = desiredVelocity;
            }
            else
            {
                transform.position += (Vector3)(desiredVelocity * Time.fixedDeltaTime);
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

        void CalculateMovement(float dt)
        {
            if (isRotating360)
            {
                Handle360Rotation(dt);
                movementDirection = Vector2.zero;
                return;
            }

            rotationTimer += dt;

            // Разворот на 360° - только в состоянии Searching
            if (rotationTimer >= rotationInterval &&
                currentState == RobotState.Searching)
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

                // Если застряли — делаем короткий "escape" манёвр
                if (isEscaping)
                {
                    movementDirection = escapeDirection;
                    isAvoiding = true;
                    return;
                }

                Vector2 baseDir = toTarget.normalized;
                lastBaseDirection = baseDir;
                movementDirection = ApplyFuzzyObstacleTurn(baseDir);

                // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ (по переднему датчику)
                float frontDist = CheckObstacleDistance(frontSensor, baseDir);
                float speedDist = Mathf.Min(frontDist, lastGabaritMinDist);
                targetSpeed = fuzzyFunction.Sentr_mass(speedDist);
                speed = Mathf.Max(0f, targetSpeed);
                return;
            }
            else if (currentState == RobotState.Searching)
            {
                // Обычный поисковой патруль
                searchTimer += dt;
                if (searchTimer > searchChangeTime)
                {
                    searchDirection = Random.insideUnitCircle.normalized;
                    searchTimer = 0f;
                    searchChangeTime = Random.Range(1f, 3f);
                }
                Vector2 baseDir = searchDirection.sqrMagnitude > 0.0001f ? searchDirection.normalized : Vector2.right;
                lastBaseDirection = baseDir;
                movementDirection = ApplyFuzzyObstacleTurn(baseDir);

                float frontDist = CheckObstacleDistance(frontSensor, baseDir);
                float speedDist = Mathf.Min(frontDist, lastGabaritMinDist);
                targetSpeed = fuzzyFunction.Sentr_mass(speedDist);
                speed = Mathf.Max(0f, targetSpeed);
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

            // Сенсоры должны задавать точки на краях робота, а направление луча — быть по курсу движения (baseDir).
            // Иначе при движении "в лоб" робот может тормозить, но не получать команду на поворот вовремя.
            Transform leftS = frontLeftSensor ? frontLeftSensor : back2Sensor;
            Transform rightS = frontRightSensor ? frontRightSensor : back1Sensor;

            RaycastHit2D frontHit = RaycastObstacle(frontSensor ? frontSensor.position : (Vector2)transform.position, baseDir);
            float dFront = frontHit.collider ? frontHit.distance : float.MaxValue;
            float dLeft = leftS ? CheckObstacleDistance(leftS, baseDir) : float.MaxValue;
            float dRight = rightS ? CheckObstacleDistance(rightS, baseDir) : float.MaxValue;

            // Аварийное объезжание: если прямо перед нами стена очень близко,
            // поворот на 45° часто всё равно "вжимает" в стену. Вместо этого едем вдоль стены (по касательной).
            float emergencyDist = obstacleAvoidDistance * 0.55f;
            if (frontHit.collider != null && dFront <= emergencyDist)
            {
                Vector2 n = frontHit.normal.sqrMagnitude > 0.0001f ? frontHit.normal.normalized : -baseDir.normalized;
                // Две касательные к нормали (вдоль стены)
                Vector2 t1 = new Vector2(-n.y, n.x).normalized;
                Vector2 t2 = -t1;

                // Предпочитаем сторону, где больше свободного места по краевым лучам.
                bool preferRight = dRight > dLeft;
                Vector2 right = new Vector2(baseDir.y, -baseDir.x).normalized; // CW = "вправо" относительно baseDir
                Vector2 preferredSide = preferRight ? right : -right;

                Vector2 t = Vector2.Dot(t1, preferredSide) >= Vector2.Dot(t2, preferredSide) ? t1 : t2;
                // И чтобы не разворачиваться назад, выбираем касательную, которая имеет неотрицательную проекцию на baseDir.
                if (Vector2.Dot(t, baseDir) < 0f) t = -t;

                isAvoiding = true;
                rotationTimer = 0f;
                lastGabaritMinDist = Mathf.Min(dLeft, dRight);
                // Сильно уводим в касательную, чтобы реально "съезжать" вдоль стены.
                return Vector2.Lerp(baseDir.normalized, t, 0.9f).normalized;
            }

            float dMin = Mathf.Min(dFront, dLeft, dRight);
            bool obstacleOnLeft = dLeft <= dRight;

            float turnAngle = fuzzyFunction.Sentr_mass_rotate(dMin, obstacleOnLeft);
            lastGabaritMinDist = Mathf.Min(dLeft, dRight);
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

            Debug.Log("Starting 360-degree rotation on spot");
        }

        void Handle360Rotation(float dt)
        {
            currentRotationTime += dt;

            if (currentRotationTime >= rotationDuration)
            {
                isRotating360 = false;
                rotationTimer = 0f;
                // 360° * rotationStartDirection возвращает тот же вектор; после сканирования
                // логичнее сменить направление поиска, чтобы не "ехать в стену" снова.
                searchDirection = Random.insideUnitCircle.normalized;
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

        float CheckObstacleDistance(Transform sensor, Vector2 castDirection)
        {
            if (sensor == null) return float.MaxValue;
            Vector2 dir = castDirection.sqrMagnitude > 0.0001f ? castDirection : lastBaseDirection;
            if (dir.sqrMagnitude < 0.0001f) return float.MaxValue;
            dir = dir.normalized;

            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, obstacleAvoidDistance * 2f, obstacleLayer);
            if (showDebug)
            {
                Debug.DrawRay(sensor.position, dir * obstacleAvoidDistance * 2f, hit.collider ? Color.red : Color.green);
            }
            return hit.collider ? hit.distance : float.MaxValue;
        }

        RaycastHit2D RaycastObstacle(Vector2 origin, Vector2 castDirection)
        {
            Vector2 dir = castDirection.sqrMagnitude > 0.0001f ? castDirection : lastBaseDirection;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir = dir.normalized;
            float dist = obstacleAvoidDistance * 2f;
            return Physics2D.Raycast(origin, dir, dist, obstacleLayer);
        }

        void ApplyRotation()
        {
            // При развороте на 360° поворачиваемся по searchDirection
            if (isRotating360 && searchDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(searchDirection.y, searchDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
            else if (movementDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
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

                bool nearObstacle = minDist < obstacleAvoidDistance * 1.1f;
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

            return minDist < obstacleAvoidDistance * 0.8f;
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

            string info = $"State: {stateStr} | Garbage: {currentGarbageType} | Target: {targetStr} | Avoiding: {isAvoiding} | Escaping: {isEscaping} | {rotationStr} | Speed: {speed:F2}";
            Debug.Log(info);
        }

        void OnDrawGizmos()
        {
            if (!showDebug) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, movementDirection * 1f);

            if (currentTarget != null)
            {
                Gizmos.color = (currentState == RobotState.GoingToTrashbin) ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
                Gizmos.DrawWireSphere(currentTarget.position, 0.3f);
            }

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, obstacleAvoidDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, searchDirection * detectionRadius);
        }
    }
}