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

        private readonly FuzzyFunction fuzzyFunction = new FuzzyFunction();

        public Transform frontSensor;
        public Transform backSensor;
        public Transform leftSensor;
        public Transform rightSensor;

        public bool showDebug = true;

        private Rigidbody2D rb;
        private Inventory inventory;
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
        private Vector2 lastBoundaryAvoidDir = Vector2.zero;
        private Vector2 boundaryAvoidance = Vector2.zero;

        private enum RobotState { Searching, GoingToGarbage, GoingToTrashbin, Unloading }
        private RobotState currentState = RobotState.Searching;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody2D component not found!");
                enabled = false;
                return;
            }

            // Сцена сейчас замораживает X/Y (m_Constraints: 7), из-за этого ИИ-движение работает только через transform,
            // что ломает физику/рейкасты. Размораживаем позиции и оставляем только FreezeRotation.
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                Debug.LogError("Inventory component not found!");
                enabled = false;
                return;
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
            if (showDebug) DebugInfo();
        }

        void FixedUpdate()
        {
            ApplyMovement();
        }

        Vector2 ForwardDir() => (Vector2)transform.up;
        Vector2 RightDir() => (Vector2)transform.right;

        void CheckBoundariesWithSensors()
        {
            // Сенсоры должны "смотреть" относительно ориентации робота, а не по осям мира.
            float frontDist = CheckBoundaryDistance(frontSensor, ForwardDir());
            float backDist = CheckBoundaryDistance(backSensor, -ForwardDir());
            float leftDist = CheckBoundaryDistance(leftSensor, -RightDir());
            float rightDist = CheckBoundaryDistance(rightSensor, RightDir());

            boundaryAvoidance = Vector2.zero;
            if (frontDist < safeZoneDistance) boundaryAvoidance += -ForwardDir() * (safeZoneDistance - frontDist);
            if (backDist < safeZoneDistance) boundaryAvoidance += ForwardDir() * (safeZoneDistance - backDist);
            if (leftDist < safeZoneDistance) boundaryAvoidance += RightDir() * (safeZoneDistance - leftDist);
            if (rightDist < safeZoneDistance) boundaryAvoidance += -RightDir() * (safeZoneDistance - rightDist);

            bool wasNearBoundary = isNearBoundary;
            isNearBoundary = boundaryAvoidance.sqrMagnitude > 0.0001f;

            if (isNearBoundary)
            {
                boundaryTimer += Time.deltaTime;
                Vector2 avoidDir = boundaryAvoidance.normalized;

                if (!wasNearBoundary ||
                    Vector2.Dot(avoidDir, lastBoundaryAvoidDir) < 0.7f ||
                    boundaryTimer >= boundaryCooldown)
                {
                    // Разворачиваемся в безопасную сторону (и чуть рандомизируем, чтобы не "пилить" вдоль стены).
                    searchDirection = avoidDir;
                    float randomAngle = Random.Range(-35f, 35f);
                    searchDirection = (Quaternion.Euler(0, 0, randomAngle) * searchDirection).normalized;

                    searchTimer = 0f;
                    boundaryTimer = 0f;
                    lastBoundaryAvoidDir = avoidDir;
                }
            }
            else
            {
                lastBoundaryAvoidDir = Vector2.zero;
                boundaryTimer = 0f;
            }
        }

        float CheckBoundaryDistance(Transform sensor, Vector2 direction)
        {
            if (sensor == null) return float.MaxValue;

            RaycastHit2D hit = Physics2D.Raycast(
                sensor.position,
                direction,
                safeZoneDistance * 2f,
                obstacleLayer
            );

            return hit.collider ? hit.distance : float.MaxValue;
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
                    float frontDist = CheckObstacleDistance(frontSensor, ForwardDir());
                    speed = fuzzyFunction.Sentr_mass(frontDist);
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
                    float frontDist = CheckObstacleDistance(frontSensor, ForwardDir());
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

            float leftDist = CheckObstacleDistance(leftSensor, -RightDir());
            float rightDist = CheckObstacleDistance(rightSensor, RightDir());
            float frontDist = CheckObstacleDistance(frontSensor, ForwardDir());

            // Простая логика: если что-то близко - отъезжаем
            if (leftDist < safeDistance)
            {
                avoidance += RightDir() * (safeDistance - leftDist);
            }

            if (rightDist < safeDistance)
            {
                avoidance += -RightDir() * (safeDistance - rightDist);
            }

            if (frontDist < obstacleAvoidDistance)
            {
                avoidance += -ForwardDir() * (obstacleAvoidDistance - frontDist);
            }

            // Усиливаем уход от границы (углы/стены) — это уменьшает "залипание" у стен.
            if (isNearBoundary)
            {
                avoidance += boundaryAvoidance * 2f;
            }

            return avoidance;
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

        float CheckObstacleDistance(Transform sensor, Vector2 direction)
        {
            if (sensor == null) return float.MaxValue;

            RaycastHit2D hit = Physics2D.Raycast(
                sensor.position,
                direction.normalized,
                obstacleAvoidDistance * 2f,
                obstacleLayer
            );

            if (showDebug)
            {
                Debug.DrawRay(sensor.position, direction * obstacleAvoidDistance * 2f,
                    hit.collider ? Color.red : Color.green);
            }

            return hit.collider ? hit.distance : float.MaxValue;
        }

        void ApplyMovement()
        {
            if (rb == null) return;
            if (isRotating360) return;
            if (movementDirection.magnitude <= 0.1f) return;

            Vector2 nextPos = rb.position + movementDirection.normalized * speed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);

            float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle - 90f);
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