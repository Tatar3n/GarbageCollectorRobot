using UnityEngine;
using System.Collections.Generic;

namespace Fuzzy
{
    public class FuzzyLogicSystem : MonoBehaviour
    {
        public float speed = 3f;
        public float detectionRadius = 5f;
        public float obstacleAvoidDistance = 1.5f;
        public float rotationInterval = 5f;
        public float safeDistance = 0.3f;
        public float rotationDuration = 1f;
        public float safeZoneDistance = 2f;
        public float boundaryCooldown = 1f;

        public LayerMask obstacleLayer;
        public LayerMask garbageLayer;

        private FuzzyFunction fuzzyFunction = new FuzzyFunction();

        public Transform frontSensor;
        public Transform backSensor;
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
            ApplyRotation();
            if (showDebug) DebugInfo();
        }

        void FixedUpdate()
        {
            // Движение выполняем в физическом тике, чтобы корректно работали коллайдеры/стены.
            if (movementDirection.magnitude > 0.1f)
            {
                desiredVelocity = movementDirection.normalized * speed;
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

        void CheckBoundariesWithSensors()
        {
            // Вычисляем “опасную” границу по сенсорам: берём ближайшее попадание и уходим по нормали стены.
            bool wasNearBoundary = isNearBoundary;
            isNearBoundary = false;

            RaycastHit2D bestHit = default;
            bool hasHit = false;
            float bestDist = float.MaxValue;
            Vector2 bestNormal = Vector2.zero;

            EvaluateBoundaryHit(frontSensor, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(backSensor, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(leftSensor, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);
            EvaluateBoundaryHit(rightSensor, ref hasHit, ref bestDist, ref bestHit, ref bestNormal);

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

        void EvaluateBoundaryHit(Transform sensor, ref bool hasHit, ref float bestDist, ref RaycastHit2D bestHit, ref Vector2 bestNormal)
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
                return;
            }

            rotationTimer += Time.deltaTime;
            if (rotationTimer >= rotationInterval && currentState == RobotState.Searching && currentTarget == null && !isNearBoundary)
            {
                Start360Rotation();
                return;
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

                // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ
                if (frontSensor != null)
                {
                    float frontDist = CheckObstacleDistance(frontSensor);
                    speed = fuzzyFunction.Sentr_mass(frontDist);
                }

                // Safe zone проверка при движении к цели: если у стены — сперва отъезжаем по searchDirection
                if (isNearBoundary)
                {
                    Debug.Log("Too close to boundary - moving away before target!");
                    movementDirection = searchDirection;
                    isAvoiding = true;
                    return;
                }

                // Простое уклонение от препятствий
                Vector2 avoidanceDir = CalculateSimpleAvoidanceDirection();
                if (avoidanceDir.magnitude > 0.1f)
                {
                    movementDirection = (toTarget.normalized + avoidanceDir).normalized;
                    isAvoiding = true;
                }
                else
                {
                    movementDirection = toTarget.normalized;
                    isAvoiding = false;
                }

                return;
            }
            else if (currentState == RobotState.Searching)
            {
                // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ
                if (frontSensor != null)
                {
                    float frontDist = CheckObstacleDistance(frontSensor);
                    speed = fuzzyFunction.Sentr_mass(frontDist);
                }

                // Если близко к границе - двигаемся в безопасном направлении
                if (isNearBoundary)
                {
                    movementDirection = searchDirection;
                    return;
                }

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

            // Простое уклонение от препятствий при поиске
            Vector2 avoidDir = CalculateSimpleAvoidanceDirection();
            if (avoidDir.magnitude > 0.1f)
            {
                movementDirection = (targetDirection + avoidDir).normalized;
                isAvoiding = true;
                rotationTimer = 0f;
            }
            else
            {
                movementDirection = targetDirection;
                isAvoiding = false;
            }
        }

        Vector2 CalculateSimpleAvoidanceDirection()
        {
            Vector2 avoidance = Vector2.zero;

            // Отталкиваемся от препятствий по нормали попадания (а не по осям мира).
            AddAvoidanceFromSensor(frontSensor, obstacleAvoidDistance, ref avoidance);
            AddAvoidanceFromSensor(leftSensor, safeDistance, ref avoidance);
            AddAvoidanceFromSensor(rightSensor, safeDistance, ref avoidance);
            AddAvoidanceFromSensor(backSensor, safeDistance, ref avoidance);

            return avoidance;
        }

        void AddAvoidanceFromSensor(Transform sensor, float thresholdDistance, ref Vector2 avoidance)
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

            float dist = Mathf.Max(hit.distance, 0.0001f);
            float weight = Mathf.Clamp01((thresholdDistance - dist) / thresholdDistance);
            Vector2 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : -dir;

            avoidance += normal * weight;
        }

        Vector2 GetSensorWorldDirection(Transform sensor)
        {
            // Сенсор — точка на корпусе. Направление “наружу” берём от центра робота к сенсору.
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
            Debug.Log("Starting 360-degree rotation");
        }

        void Handle360Rotation()
        {
            currentRotationTime += Time.deltaTime;

            if (currentRotationTime >= rotationDuration)
            {
                isRotating360 = false;
                rotationTimer = 0f;
                searchDirection = Quaternion.Euler(0, 0, 360f) * rotationStartDirection;
                Debug.Log("360-degree rotation completed");
                return;
            }

            float progress = currentRotationTime / rotationDuration;
            float rotationAngle = 360f * progress;

            searchDirection = Quaternion.Euler(0, 0, rotationAngle) * rotationStartDirection;
            movementDirection = searchDirection;
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
            if (movementDirection.magnitude > 0.1f)
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

            string rotationStr = isRotating360 ? $"Rotating ({(currentRotationTime / rotationDuration) * 100:F0}%)" : "Not rotating";

            string info = $"State: {stateStr} | Garbage: {currentGarbageType} | Target: {targetStr} | Avoiding: {isAvoiding} | NearBoundary: {isNearBoundary} | {rotationStr} | Speed: {speed:F2}";
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

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, safeDistance);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, safeZoneDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, searchDirection * detectionRadius);
        }
    }
}