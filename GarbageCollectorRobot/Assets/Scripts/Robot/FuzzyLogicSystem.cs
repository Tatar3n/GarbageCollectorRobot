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
        private Vector2 desiredVelocity = Vector2.zero;
        private float targetSpeed = 0f;
        private float lastGabaritMinDist = float.MaxValue;

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
            CalculateMovement();
            ApplyRotation();
            if (showDebug) DebugInfo();
        }

        void FixedUpdate()
        {
            // Движение выполняем в физическом тике, чтобы корректно работали коллайдеры/стены.
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
                        // SearchDirection используем только когда датчики "ничего не видят".
                        if (ObstacleSensorsSeeNothing())
                        {
                            FindGarbageWithRay();
                        }
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
            bool isSearchingNow = currentState == RobotState.Searching;
            bool sensorsClear = isSearchingNow && ObstacleSensorsSeeNothing();

            // Разворот на 360° - только в состоянии Searching
            if (rotationTimer >= rotationInterval &&
                isSearchingNow &&
                sensorsClear)
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

                Vector2 baseDir = toTarget.normalized;
                movementDirection = ApplyFuzzyObstacleTurn(baseDir);

                // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ (по переднему датчику)
                float frontDist = CheckObstacleDistance(frontSensor);
                float speedDist = Mathf.Min(frontDist, lastGabaritMinDist);
                targetSpeed = fuzzyFunction.Sentr_mass(speedDist);
                speed = Mathf.Max(0f, targetSpeed);
                return;
            }
            else if (isSearchingNow)
            {
                // SearchDirection должен быть активен ТОЛЬКО когда датчики не видят препятствий.
                // Если датчики что-то видят — SearchDirection не обновляем и не используем как базовый курс.
                if (sensorsClear)
                {
                    // Обычный поисковой патруль
                    searchTimer += Time.deltaTime;
                    if (searchTimer > searchChangeTime)
                    {
                        searchDirection = Random.insideUnitCircle.normalized;
                        searchTimer = 0f;
                        searchChangeTime = Random.Range(1f, 3f);
                    }
                }
                Vector2 baseDir;
                if (sensorsClear)
                {
                    baseDir = searchDirection.sqrMagnitude > 0.0001f ? searchDirection.normalized : Vector2.right;
                }
                else
                {
                    // "Другая" логика тут не нужна: просто продолжаем текущий курс и даём Avoidance отработать.
                    // Главное — не подмешивать SearchDirection, пока датчики видят препятствие.
                    baseDir = movementDirection.sqrMagnitude > 0.0001f ? movementDirection.normalized : Vector2.right;
                }
                movementDirection = ApplyFuzzyObstacleTurn(baseDir);

                float frontDist = CheckObstacleDistance(frontSensor);
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

        bool ObstacleSensorsSeeNothing()
        {
            // "Датчики ничего не видят" = ни один сенсор не попал лучом в obstacleLayer.
            // Используем их "наружное" направление (как и в CheckObstacleDistance).
            return !SensorHitsObstacle(frontSensor) &&
                   !SensorHitsObstacle(back1Sensor) &&
                   !SensorHitsObstacle(back2Sensor);
        }

        bool SensorHitsObstacle(Transform sensor)
        {
            if (sensor == null) return false;
            Vector2 dir = GetSensorWorldDirection(sensor);
            if (dir.sqrMagnitude < 0.0001f) return false;
            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, obstacleAvoidDistance * 2f, obstacleLayer);
            return hit.collider != null;
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

            string info = $"State: {stateStr} | Garbage: {currentGarbageType} | Target: {targetStr} | Avoiding: {isAvoiding} | {rotationStr} | Speed: {speed:F2}";
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
            // searchDirection — это "поисковый" вектор. Показываем его только когда реально в поиске/скане.
            if (currentState == RobotState.Searching || isRotating360)
            {
                Gizmos.DrawRay(transform.position, searchDirection * detectionRadius);
            }
        }
    }
}