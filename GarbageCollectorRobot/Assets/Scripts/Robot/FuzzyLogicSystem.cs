using UnityEngine;
using System.Collections;

namespace Fuzzy
{
    public class FuzzyLogicSystem : MonoBehaviour
    {
        [Header("Sensors")]
        public Transform frontSensor;
        public Transform leftSensor;
        public Transform rightSensor;
        
        [Header("Sensor Settings")]
        public float sensorLength = 2f;
        [Tooltip("Угол поворота левого датчика")]
        public float leftSensorAngle = 45f;
        [Tooltip("Угол поворота правого датчика")]
        public float rightSensorAngle = -45f;
        
        [Header("Movement Settings")]
        public float baseSpeed = 3f;
        public float rotationSpeed = 180f; 
        public bool hasTrash = false;
        

        [Header("Goal Seeking")]
        public bool enableTrashBinSeeking = true;
        [Tooltip("Насколько сильно тянуть поворот в сторону мусорки")]
        public float seekTurnMultiplier = 1f;
        [Tooltip("Как часто пересканировать мусорки на сцене")]
        public float binScanInterval = 1.0f;
        [Tooltip("Соответствие угла до цели и 'дистанции' для той же нечёткой логики поворота")]
        public float seekDistanceMin = 0.5f;  
        public float seekDistanceMax = 2.5f; 
        [Tooltip("Угол, при котором поворот к цели считается максимальным")]
        public float seekAngleForMaxTurn = 90f;

        [Header("Turn Memory")]
        public float turnMemoryTime = 0.5f; 
        public float memoryClearDelay = 0.05f;
        
        [Header("Smoothing")]
        public float directionSmoothing = 10f;
        public float speedSmoothing = 8f;
        public float rotationSmoothing = 5f;

        [Header("In-Game Sensor Rendering")]
        [Tooltip("Рисовать лучи датчиков в GameView (через LineRenderer)")]
        public bool renderSensorsInGame = true;
        [Tooltip("Толщина линий датчиков")]
        public float sensorLineWidth = 0.03f;
        [Tooltip("Sorting Order для линий датчиков (чтобы было видно поверх спрайтов)")]
        public int sensorLineSortingOrder = 50;

        public LayerMask obstacleLayer;
        public bool showDebug = true;

        private readonly FuzzyFunction fuzzyFunction = new FuzzyFunction();
        private Rigidbody2D rb;
        private Vector2 movementDirection = Vector2.zero;
        private Vector2 smoothedMoveDirection = Vector2.zero;
        private float targetSpeed = 0f;
        private float targetRotation = 0f; 
        private float currentRotation = 0f; 
        
        private Inventory inventory;
        private Types.GType lastGarbageType = Types.GType.None;
        private TrashBin[] cachedBins = null;
        private float binScanTimer = 0f;
        private TrashBin currentTargetBin = null;
        private float turnMemoryTimer = 0f;
        private float rememberedRotation = 0f; 
        private bool hasTurnMemory = false;
        private bool isInTurnMemoryMode = false; 
        private Coroutine clearMemoryCoroutine = null;
        private float lastObstacleTime = 0f;
        private bool shouldClearMemory = false;

        private struct SensorReading
        {
            public bool hit;
            public float distance;
            public Vector2 from;
            public Vector2 to;
            public Vector2 dir;
        }

        private LineRenderer frontLine;
        private LineRenderer leftLine;
        private LineRenderer rightLine;

        public float CurrentTurnAngle => currentRotation;
        public float TargetTurnAngle => targetRotation;
        public float RememberedTurnAngle => rememberedRotation;
        public bool HasTurnMemory => hasTurnMemory;
        public float TurnMemoryTimer => turnMemoryTimer;
        public float BaseSpeed => baseSpeed;
        public float TargetSpeed => targetSpeed;

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
                manualMove.enabled = false;

            inventory = GetComponentInParent<Inventory>();
            if (inventory == null)
                inventory = GetComponent<Inventory>();

            smoothedMoveDirection = Vector2.right;
            targetSpeed = baseSpeed;

            if (renderSensorsInGame)
            {
                frontLine = CreateSensorLineRenderer("SensorLine_Front", Color.green);
                leftLine = CreateSensorLineRenderer("SensorLine_Left", Color.green);
                rightLine = CreateSensorLineRenderer("SensorLine_Right", Color.green);
            }
        }

        [System.Obsolete]
        void Update()
        {
            UpdateTurnMemory();
            UpdateHasTrashAndTarget();
            CalculateMovement();
            SmoothMovement();
            ApplyRotationOnSpot();
            ApplyForwardMovement();
            UpdateSensorLines();
            
            CheckMemoryClearDelay();
        }

        void CheckMemoryClearDelay()
        {
            if (shouldClearMemory && Time.time - lastObstacleTime >= memoryClearDelay)
            {
                ForceClearTurnMemory();
                shouldClearMemory = false;
            }
        }

        [System.Obsolete]
        void UpdateHasTrashAndTarget()
        {
            Types.GType currentType = Types.GType.None;
            if (inventory != null)
                currentType = inventory.getCell();

            hasTrash = currentType != Types.GType.None;

            if (!hasTrash || !enableTrashBinSeeking)
            {
                currentTargetBin = null;
                lastGarbageType = currentType;
                return;
            }

            binScanTimer -= Time.deltaTime;
            if (cachedBins == null || binScanTimer <= 0f || currentType != lastGarbageType || currentTargetBin == null)
            {
                cachedBins = FindObjectsOfType<TrashBin>();
                binScanTimer = Mathf.Max(0.1f, binScanInterval);
                currentTargetBin = FindNearestBinForType(currentType, cachedBins);
                lastGarbageType = currentType;
            }
        }

        TrashBin FindNearestBinForType(Types.GType type, TrashBin[] bins)
        {
            if (bins == null) return null;

            TrashBin best = null;
            float bestSqr = float.MaxValue;
            Vector2 pos = transform.position;

            for (int i = 0; i < bins.Length; i++)
            {
                TrashBin b = bins[i];
                if (b == null) continue;
                if (b.garbageType != type) continue;

                float sqr = ((Vector2)b.transform.position - pos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = b;
                }
            }

            return best;
        }

        [System.Obsolete]
    void CalculateMovement()
    {
        SensorReading front = GetSensorReading(frontSensor);
        SensorReading right = rightSensor ? GetSensorReading(rightSensor) : default;
        SensorReading left = leftSensor ? GetSensorReading(leftSensor) : default;

        float frontDist = front.distance;
        float rightDist = rightSensor ? right.distance : float.MaxValue;
        float leftDist = leftSensor ? left.distance : float.MaxValue;
        
        bool hasObstacleInFront = front.hit;
        bool hasObstacleOnRight = rightSensor && right.hit;
        bool hasObstacleOnLeft = leftSensor && left.hit;
        bool hasAnyObstacle = hasObstacleInFront || hasObstacleOnRight || hasObstacleOnLeft;
        
        if (hasAnyObstacle)
        {
            lastObstacleTime = Time.time;
            shouldClearMemory = false;
            
            if (clearMemoryCoroutine != null)
            {
                StopCoroutine(clearMemoryCoroutine);
                clearMemoryCoroutine = null;
            }
        }
        else if (hasTurnMemory && !shouldClearMemory)
        {
            shouldClearMemory = true;
            
        }
        
        float minDistance = Mathf.Min(frontDist, rightDist - 0.32f, leftDist - 0.32f);
        targetSpeed = fuzzyFunction.Sentr_mass(minDistance);
        
        float dMin = 0.0f;
        bool isLeft = true;
        float obstacleTurnAngle = 0.0f;
        
        obstacleTurnAngle = fuzzyFunction.Sentr_mass_rotate(rightDist, leftDist, 0f);
        
        float seekTurnAngle = 0f;
        bool hasSeek = false;
        float signedAngle = 0f;
        
        if (hasTrash && enableTrashBinSeeking && currentTargetBin != null)
        {
            Vector2 toBin = (Vector2)currentTargetBin.transform.position - (Vector2)transform.position;
            if (toBin.sqrMagnitude > 0.0001f)
            {
                Vector2 desiredDir = toBin.normalized;
                Vector2 forward = transform.up;
                // SignedAngle возвращает положительный угол если цель СЛЕВА
                // Нам нужно: положительный = цель СПРАВА, поэтому инвертируем
                signedAngle = -Vector2.SignedAngle(forward, desiredDir);
                
                // Передаём угол до мусорки в нечёткую логику
                obstacleTurnAngle = fuzzyFunction.Sentr_mass_rotate(rightDist, leftDist, signedAngle);
                hasSeek = true;
            }
        }
        
        if (hasTrash && enableTrashBinSeeking && currentTargetBin != null)
        {
            targetRotation = obstacleTurnAngle;
            hasTurnMemory = false;
            isInTurnMemoryMode = false;
            rememberedRotation = 0f;
            turnMemoryTimer = 0f;
        }
        else
        {
            float finalTurnAngle = 0f;
            
            if (isInTurnMemoryMode && hasTurnMemory && !shouldClearMemory)
            {
                finalTurnAngle = rememberedRotation;
                
            }
            else
            {
                finalTurnAngle = obstacleTurnAngle;
                
                if (Mathf.Abs(finalTurnAngle) > 5f && hasAnyObstacle)
                {
                    rememberedRotation = Mathf.Clamp(finalTurnAngle, -128f, 128f);
                    turnMemoryTimer = turnMemoryTime + Random.Range(0f, 0.1f);
                    hasTurnMemory = true;
                    isInTurnMemoryMode = true;
                    
                }
            }
            
            targetRotation = finalTurnAngle;
        }
        
        if (targetRotation < 0.01f && targetSpeed < 0.01f)
        {
            targetRotation = 180f;
            targetSpeed = 0.01f;
        }
        
        // Debug логирование только при необычных значениях
        if (showDebug && Mathf.Abs(targetRotation) > 100f)
        {
            Debug.Log($"[FuzzyNav] Dist: F={frontDist:F1}, L={leftDist:F1}, R={rightDist:F1}");
            Debug.Log($"[FuzzyNav] Angle to target: {signedAngle:F1}°, Seek turn: {seekTurnAngle:F1}°, Obstacle turn: {obstacleTurnAngle:F1}°");
            Debug.Log($"[FuzzyNav] Final rotation: {targetRotation:F1}°, Mode: {(hasTrash ? "Delivering" : "Searching")}");
        }
    }

        void ForceClearTurnMemory()
        {
            hasTurnMemory = false;
            turnMemoryTimer = 0f;
            rememberedRotation = 0f;
            isInTurnMemoryMode = false;
            
            if (clearMemoryCoroutine != null)
            {
                StopCoroutine(clearMemoryCoroutine);
                clearMemoryCoroutine = null;
            }
            
            shouldClearMemory = false;
            
        }

        void UpdateTurnMemory()
        {
            if (hasTurnMemory && !shouldClearMemory)
            {
                turnMemoryTimer -= Time.deltaTime;
                if (turnMemoryTimer <= 0f)
                {
                    ForceClearTurnMemory();
                }
            }
        }

        void SmoothMovement()
        {
            float dt = Time.deltaTime;
            float spdAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, speedSmoothing) * dt);
            float rotAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothing) * dt);
            
            baseSpeed = Mathf.Lerp(baseSpeed, targetSpeed, spdAlpha);
            baseSpeed = Mathf.Max(0f, baseSpeed);
            
            if (!isInTurnMemoryMode)
                currentRotation = Mathf.Lerp(currentRotation, targetRotation, rotAlpha);
            else
                currentRotation = rememberedRotation;
        }

        void ApplyRotationOnSpot()
        {
            if (Mathf.Abs(currentRotation) > 0.1f)
            {
                float rotationThisFrame = currentRotation * rotationSpeed * Time.deltaTime;
                transform.Rotate(0, 0, -rotationThisFrame); 
                
                
            }
        }

        void ApplyForwardMovement()
        {
            Vector2 forward = transform.up;
            
            if (rb != null)
                rb.velocity = forward * baseSpeed;
            else
                transform.position += (Vector3)(forward * baseSpeed * Time.deltaTime);
        }

        float CheckObstacleDistance(Transform sensor)
        {
            if (sensor == null) return float.MaxValue;    
            Vector2 dir = transform.up; 
            
            if (sensor == leftSensor)
            {
                dir = Quaternion.Euler(0, 0, leftSensorAngle) * dir;
            }
            else if (sensor == rightSensor)
            {
                dir = Quaternion.Euler(0, 0, rightSensorAngle) * dir;
            }
            
            if (dir.sqrMagnitude < 0.0001f) return float.MaxValue;
            float checkDistance = sensorLength; 
            RaycastHit2D hit = Physics2D.Raycast(sensor.position, dir, checkDistance, obstacleLayer);  
            if (showDebug)
            {
                Color rayColor = hit.collider ? Color.red : Color.green;
                Debug.DrawRay(sensor.position, dir * checkDistance, rayColor);
            } 
            return hit.collider ? hit.distance : float.MaxValue;
        }

        private SensorReading GetSensorReading(Transform sensor)
        {
            SensorReading r = default;
            if (sensor == null)
            {
                r.hit = false;
                r.distance = float.MaxValue;
                return r;
            }

            Vector2 dir = transform.up;
            if (sensor == leftSensor)
                dir = Quaternion.Euler(0, 0, leftSensorAngle) * dir;
            else if (sensor == rightSensor)
                dir = Quaternion.Euler(0, 0, rightSensorAngle) * dir;

            if (dir.sqrMagnitude < 0.0001f)
            {
                r.hit = false;
                r.distance = float.MaxValue;
                return r;
            }

            float checkDistance = sensorLength;
            Vector2 from = sensor.position;
            RaycastHit2D hit = Physics2D.Raycast(from, dir, checkDistance, obstacleLayer);

            r.hit = hit.collider != null;
            r.distance = r.hit ? hit.distance : float.MaxValue;
            r.from = from;
            r.dir = dir;
            r.to = r.hit ? hit.point : (from + dir * checkDistance);

            if (showDebug)
            {
                Color rayColor = r.hit ? Color.red : Color.green;
                Debug.DrawRay(from, dir * checkDistance, rayColor);
            }

            return r;
        }

        private LineRenderer CreateSensorLineRenderer(string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = sensorLineWidth;
            lr.endWidth = sensorLineWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = sensorLineSortingOrder;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private void UpdateSensorLines()
        {
            if (!renderSensorsInGame) return;

            UpdateSensorLine(frontLine, GetSensorReading(frontSensor));
            UpdateSensorLine(leftLine, GetSensorReading(leftSensor));
            UpdateSensorLine(rightLine, GetSensorReading(rightSensor));
        }

        private void UpdateSensorLine(LineRenderer lr, SensorReading r)
        {
            if (lr == null) return;
            if (!r.hit && r.distance == float.MaxValue && r.from == Vector2.zero && r.to == Vector2.zero)
            {
                lr.enabled = false;
                return;
            }

            lr.enabled = true;
            lr.startWidth = sensorLineWidth;
            lr.endWidth = sensorLineWidth;

            Color c = r.hit ? Color.red : Color.green;
            lr.startColor = c;
            lr.endColor = c;
            lr.SetPosition(0, r.from);
            lr.SetPosition(1, r.to);
        }

        void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying) return;
            DrawSensorGizmo(frontSensor, Color.blue);
            DrawSensorGizmo(leftSensor, Color.yellow);
            DrawSensorGizmo(rightSensor, Color.yellow);        
            DrawTurnMemoryGizmo();
            if (hasTrash && enableTrashBinSeeking && currentTargetBin != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentTargetBin.transform.position);
                Gizmos.DrawSphere(currentTargetBin.transform.position, 0.12f);
            }
        }

        void DrawSensorGizmo(Transform sensor, Color color)
        {
            if (sensor == null) return;           
            Gizmos.color = color;
            Vector2 dir = transform.up;
            
            if (sensor == leftSensor)
            {
                dir = Quaternion.Euler(0, 0, leftSensorAngle) * dir;
            }
            else if (sensor == rightSensor)
            {
                dir = Quaternion.Euler(0, 0, rightSensorAngle) * dir;
            }
            
            Gizmos.DrawLine(sensor.position, sensor.position + (Vector3)dir * sensorLength);         
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(sensor.position, 0.05f);
        }

        void DrawTurnMemoryGizmo()
        {
            if (!hasTurnMemory) return;
            float memoryStrength = Mathf.Clamp01(turnMemoryTimer / turnMemoryTime);
            Gizmos.color = Color.Lerp(Color.red, Color.green, memoryStrength);
            Vector3 center = transform.position;
            float radius = 0.8f;
            Vector3 startDir = transform.up * radius;
            Vector3 endDir = Quaternion.Euler(0, 0, -rememberedRotation) * transform.up * radius;
            int segments = 20;
            float angleStep = rememberedRotation / segments;
            Vector3 prevPoint = center + startDir;  
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i;
                Vector3 currentDir = Quaternion.Euler(0, 0, -angle) * transform.up * radius;
                Vector3 currentPoint = center + currentDir;         
                Gizmos.DrawLine(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 1.2f, 
                $"MEMORY: {rememberedRotation:F1}°\nTime: {turnMemoryTimer:F2}s");
            #endif
        }
    }
}