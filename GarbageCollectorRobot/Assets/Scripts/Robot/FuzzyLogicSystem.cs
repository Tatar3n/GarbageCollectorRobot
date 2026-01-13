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
        [Tooltip("Задержка после исчезновения препятствий перед включением тяги к цели")]
        public float seekObstacleDelay = 0.1f;

        [Header("Turn Memory")]
        public float turnMemoryTime = 0.5f; 
        public float memoryClearDelay = 0.05f;
        
        [Header("Smoothing")]
        public float directionSmoothing = 10f;
        public float speedSmoothing = 8f;
        public float rotationSmoothing = 5f;

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

        private float lastObstacleSeenTime = 0f;
        private bool canSeekTrashBin = false;

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
            
            lastObstacleSeenTime = Time.time;
        }

        [System.Obsolete]
        void Update()
        {
            UpdateTurnMemory();
            UpdateHasTrashAndTarget();
            UpdateSeekPermission(); 
            CalculateMovement();
            SmoothMovement();
            ApplyRotationOnSpot();
            ApplyForwardMovement();
            
            CheckMemoryClearDelay();
        }

        void UpdateSeekPermission()
        {
            float frontDist = CheckObstacleDistance(frontSensor);
            float rightDist = rightSensor ? CheckObstacleDistance(rightSensor) : float.MaxValue;
            float leftDist = leftSensor ? CheckObstacleDistance(leftSensor) : float.MaxValue;
            
            bool hasObstacleInFront = frontDist < float.MaxValue;
            bool hasObstacleOnRight = rightDist < float.MaxValue;
            bool hasObstacleOnLeft = leftDist < float.MaxValue;
            bool hasAnyObstacle = hasObstacleInFront || hasObstacleOnRight || hasObstacleOnLeft;
            
            if (hasAnyObstacle)
            {
                lastObstacleSeenTime = Time.time;
                canSeekTrashBin = false;
                Debug.Log($"Obstacle detected - seeking disabled. Last seen: {lastObstacleSeenTime:F2}");
            }
            else
            {
                float timeSinceLastObstacle = Time.time - lastObstacleSeenTime;
                if (timeSinceLastObstacle >= seekObstacleDelay)
                {
                    canSeekTrashBin = true;
                    Debug.Log($"No obstacles for {timeSinceLastObstacle:F2}s - seeking enabled");
                }
                else
                {
                    canSeekTrashBin = false;
                    Debug.Log($"Waiting for obstacle delay: {seekObstacleDelay - timeSinceLastObstacle:F2}s remaining");
                }
            }
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
            float frontDist = CheckObstacleDistance(frontSensor);
            float rightDist = rightSensor ? CheckObstacleDistance(rightSensor) : float.MaxValue;
            float leftDist = leftSensor ? CheckObstacleDistance(leftSensor) : float.MaxValue;
            
            bool hasObstacleInFront = frontDist < float.MaxValue;
            bool hasObstacleOnRight = rightDist < float.MaxValue;
            bool hasObstacleOnLeft = leftDist < float.MaxValue;
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
                Debug.Log($"No obstacles detected - memory will clear in {memoryClearDelay:F2}s");
            }
            
            float minDistance = Mathf.Min(frontDist, rightDist - 0.32f, leftDist - 0.32f);
            targetSpeed = fuzzyFunction.Sentr_mass(minDistance);
            
            float dMin = 0.0f;
            bool isLeft = true;
            float obstacleTurnAngle = 0.0f;
            
            if(leftDist - rightDist > 0) 
            {
                obstacleTurnAngle = fuzzyFunction.Sentr_mass_rotate(rightDist, isLeft, hasTrash);
                dMin = rightDist;
            }
            else 
            {
                obstacleTurnAngle = fuzzyFunction.Sentr_mass_rotate(leftDist, !isLeft, hasTrash);
                dMin = leftDist;
            }

            float seekTurnAngle = 0f;
            bool hasSeek = false;
            
            if (hasTrash && enableTrashBinSeeking && currentTargetBin != null && canSeekTrashBin)
            {
                Vector2 toBin = (Vector2)currentTargetBin.transform.position - (Vector2)transform.position;
                if (toBin.sqrMagnitude > 0.0001f)
                {
                    Vector2 desiredDir = toBin.normalized;
                    Vector2 forward = transform.up;
                    float signedAngle = Vector2.SignedAngle(forward, desiredDir);
                    float absAngle = Mathf.Abs(signedAngle);
                    bool goalIsLeft = signedAngle > 0f;
                    float t = Mathf.Clamp01(absAngle / Mathf.Max(1f, seekAngleForMaxTurn));
                    float goalD = Mathf.Lerp(seekDistanceMax, seekDistanceMin, t);

                    seekTurnAngle = fuzzyFunction.Sentr_mass_rotate(goalD, goalIsLeft, true) * seekTurnMultiplier;
                    hasSeek = true;
                    
                    Debug.Log($"Seeking trash bin: angle={signedAngle:F1}°, seekTurn={seekTurnAngle:F1}°, canSeek={canSeekTrashBin}");
                }
            }
            else if (hasTrash && currentTargetBin != null && !canSeekTrashBin)
            {
                Debug.Log($"Seeking to trash bin DISABLED - waiting for obstacle delay or no permission");
            }
            
            float finalTurnAngle = 0f;
            
            if (isInTurnMemoryMode && hasTurnMemory && !shouldClearMemory)
            {
                finalTurnAngle = rememberedRotation;
                Debug.Log($"Using memory turn: {finalTurnAngle:F1}°, Timer: {turnMemoryTimer:F2}");
            }
            else
            {
                if (hasSeek)
                {
                    float obstacleProximity = 1f - Mathf.InverseLerp(0.7f, 2.5f, Mathf.Min(dMin, 2.5f));
                    obstacleProximity = Mathf.Clamp01(obstacleProximity);
                    finalTurnAngle = Mathf.Lerp(seekTurnAngle, obstacleTurnAngle, obstacleProximity);
                }
                else
                    finalTurnAngle = obstacleTurnAngle;
                
                if (Mathf.Abs(finalTurnAngle) > 5f && hasAnyObstacle)
                {
                    rememberedRotation = Mathf.Clamp(finalTurnAngle, -128f, 128f);
                    turnMemoryTimer = turnMemoryTime;
                    hasTurnMemory = true;
                    isInTurnMemoryMode = true;
                    Debug.Log($"Memorized CLAMPED turn: {rememberedRotation:F1}° for {turnMemoryTime}s");
                }
                
                targetRotation = finalTurnAngle;
            }
            
            Debug.Log($"Distances: F{frontDist:F1}, L{leftDist:F1}, R{rightDist:F1}");
            Debug.Log($"Obstacle turn: {obstacleTurnAngle:F1}°, Seek: {(hasSeek ? seekTurnAngle.ToString("F1") : "n/a")}°, Final: {finalTurnAngle:F1}°, Speed: {targetSpeed:F1}");
            Debug.Log($"Seek permission: {canSeekTrashBin}, Time since last obstacle: {Time.time - lastObstacleSeenTime:F2}s");
        }

        IEnumerator ClearMemoryWithDelay()
        {
            yield return new WaitForSeconds(memoryClearDelay);
            
            if (hasTurnMemory)
            {
                ForceClearTurnMemory();
                Debug.Log($"Memory cleared after {memoryClearDelay:F2}s delay (no obstacles)");
            }
            
            clearMemoryCoroutine = null;
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
            Debug.Log("Turn memory force cleared");
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
                
                Debug.Log($"Rotating on spot: {rotationThisFrame:F1}°/s, Total: {currentRotation:F1}°");
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

        void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying) return;
            DrawSensorGizmo(frontSensor, Color.blue);
            DrawSensorGizmo(leftSensor, Color.yellow);
            DrawSensorGizmo(rightSensor, Color.yellow);        
            DrawTurnMemoryGizmo();
            DrawSeekStatusGizmo();
            
            if (hasTrash && enableTrashBinSeeking && currentTargetBin != null && canSeekTrashBin)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentTargetBin.transform.position);
                Gizmos.DrawSphere(currentTargetBin.transform.position, 0.12f);
            }
            else if (hasTrash && currentTargetBin != null)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, currentTargetBin.transform.position);
                Gizmos.DrawWireSphere(currentTargetBin.transform.position, 0.1f);
            }
        }

        void DrawSeekStatusGizmo()
        {
            Vector3 center = transform.position + transform.up * 0.3f;
            
            if (hasTrash)
            {
                if (canSeekTrashBin)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(center, 0.15f);
                    Gizmos.DrawSphere(center, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(center, 0.15f);
                    
                    float progress = Mathf.Clamp01((Time.time - lastObstacleSeenTime) / seekObstacleDelay);
                    if (progress < 1f)
                    {
                        Vector3 from = center + Vector3.up * 0.15f;
                        Vector3 to = center + Quaternion.Euler(0, 0, -360 * progress) * Vector3.up * 0.15f;
                        Gizmos.DrawLine(center, from);
                        Gizmos.DrawLine(center, to);
                    }
                }
                
                #if UNITY_EDITOR
                string statusText = canSeekTrashBin ? "CAN SEEK" : $"WAIT: {seekObstacleDelay - (Time.time - lastObstacleSeenTime):F1}s";
                UnityEditor.Handles.Label(center + Vector3.up * 0.3f, statusText);
                #endif
            }
        }

        void DrawSensorGizmo(Transform sensor, Color color)
        {
            if (sensor == null) return;           
            Gizmos.color = color;
            Vector2 dir = transform.up;
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