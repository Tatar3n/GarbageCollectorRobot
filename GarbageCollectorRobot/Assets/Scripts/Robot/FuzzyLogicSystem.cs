using UnityEngine;

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
        public float rotationSpeed = 180f; // Скорость вращения на месте (град/сек)
        public bool hasTrash = false;
        
        [Header("Turn Memory")]
        public float turnMemoryTime = 0.5f; // Фиксированные 0.5 секунды памяти
        
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
        private float targetRotation = 0f; // Целевой угол поворота от нечеткой логики
        private float currentRotation = 0f; // Текущий угол поворота (сглаженный)
        
        // Память поворота
        private float turnMemoryTimer = 0f;
        private float rememberedRotation = 0f; // Запомненный угол поворота
        private bool hasTurnMemory = false;
        private bool isInTurnMemoryMode = false; // Режим "держим поворот"

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

            smoothedMoveDirection = Vector2.right;
            targetSpeed = baseSpeed;
        }

        void Update()
        {
            // Обновляем таймер памяти
            UpdateTurnMemory();
            
            CalculateMovement();
            SmoothMovement();
            ApplyRotationOnSpot(); // Поворачиваем на месте
            ApplyForwardMovement();
        }

        void UpdateTurnMemory()
        {
            if (hasTurnMemory)
            {
                turnMemoryTimer -= Time.deltaTime;
                if (turnMemoryTimer <= 0f)
                {
                    hasTurnMemory = false;
                    rememberedRotation = 0f;
                    isInTurnMemoryMode = false;
                }
            }
        }

        void CalculateMovement()
        {
            // Получаем расстояния от всех датчиков
            float frontDist = CheckObstacleDistance(frontSensor);
            float rightDist = rightSensor ? CheckObstacleDistance(rightSensor) : float.MaxValue;
            float leftDist = leftSensor ? CheckObstacleDistance(leftSensor) : float.MaxValue;
            
            // Находим минимальное расстояние для скорости
            float minDistance = Mathf.Min(frontDist, rightDist, leftDist);
            targetSpeed = fuzzyFunction.Sentr_mass(minDistance);
            
            // ОРИГИНАЛЬНАЯ ЛОГИКА ПОВОРОТА из нечеткой логики
            float dMin = 0.0f;
            bool isLeft = true;

            if (Mathf.Abs(leftDist - rightDist) > 0.8f)
            {
                dMin = Mathf.Min(leftDist, rightDist);
                isLeft = leftDist <= rightDist;
            }
            else
            {
                dMin = Mathf.Min(leftDist, rightDist);
                isLeft = true;
            }

            // Получаем угол поворота от нечёткой логики
            float fuzzyTurnAngle = fuzzyFunction.Sentr_mass_rotate(dMin, isLeft, hasTrash);
            
            float finalTurnAngle = 0f;
            
            // Если есть активная память - используем её
            if (isInTurnMemoryMode && hasTurnMemory)
            {
                finalTurnAngle = rememberedRotation;
                Debug.Log($"Using memory turn: {finalTurnAngle:F1}°, Timer: {turnMemoryTimer:F2}");
            }
            else
            {
                // Используем угол от нечеткой логики
                finalTurnAngle = fuzzyTurnAngle;
                
                // Если угол значительный - запоминаем его на 0.5 секунды
                if (Mathf.Abs(fuzzyTurnAngle) > 5f)
                {
                    // МГНОВЕННО запоминаем поворот
                    rememberedRotation = fuzzyTurnAngle;
                    turnMemoryTimer = turnMemoryTime;
                    hasTurnMemory = true;
                    isInTurnMemoryMode = true;
                    Debug.Log($"Memorized fuzzy turn: {fuzzyTurnAngle:F1}° for {turnMemoryTime}s");
                }
                
                targetRotation = finalTurnAngle;
            }
            
            Debug.Log($"Distances: F{frontDist:F1}, L{leftDist:F1}, R{rightDist:F1}");
            Debug.Log($"Fuzzy turn: {fuzzyTurnAngle:F1}°, Final: {finalTurnAngle:F1}°, Speed: {targetSpeed:F1}");
        }

        void SmoothMovement()
        {
            float dt = Time.deltaTime;
            float spdAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, speedSmoothing) * dt);
            float rotAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothing) * dt);
            
            // Сглаживаем скорость
            baseSpeed = Mathf.Lerp(baseSpeed, targetSpeed, spdAlpha);
            baseSpeed = Mathf.Max(0f, baseSpeed);
            
            // Сглаживаем угол поворота (только если не в режиме памяти)
            if (!isInTurnMemoryMode)
            {
                currentRotation = Mathf.Lerp(currentRotation, targetRotation, rotAlpha);
            }
            else
            {
                // В режиме памяти используем запомненный угол без сглаживания
                currentRotation = rememberedRotation;
            }
        }

        void ApplyRotationOnSpot()
        {
            // Поворачиваем на месте (без движения вперед)
            if (Mathf.Abs(currentRotation) > 0.1f)
            {
                float rotationThisFrame = currentRotation * rotationSpeed * Time.deltaTime;
                transform.Rotate(0, 0, -rotationThisFrame); // Поворачиваем вокруг оси Z
                
                Debug.Log($"Rotating on spot: {rotationThisFrame:F1}°/s, Total: {currentRotation:F1}°");
            }
        }

        void ApplyForwardMovement()
        {
            // Движемся только вперед (после поворота на месте)
            Vector2 forward = transform.up; // В 2D вперед обычно это up
            
            if (rb != null)
            {
                rb.velocity = forward * baseSpeed;
            }
            else
            {
                transform.position += (Vector3)(forward * baseSpeed * Time.deltaTime);
            }
        }

        float CheckObstacleDistance(Transform sensor)
        {
            if (sensor == null) return float.MaxValue;
            
            Vector2 dir = transform.up; // Все датчики смотрят вперед
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
            
            // Рисуем индикатор памяти поворота
            float memoryStrength = Mathf.Clamp01(turnMemoryTimer / turnMemoryTime);
            Gizmos.color = Color.Lerp(Color.red, Color.green, memoryStrength);
            
            // Дуга, показывающая угол и оставшееся время
            Vector3 center = transform.position;
            float radius = 0.8f;
            
            // Начало дуги (прямо вперед)
            Vector3 startDir = transform.up * radius;
            
            // Конец дуги (запомненный угол)
            Vector3 endDir = Quaternion.Euler(0, 0, -rememberedRotation) * transform.up * radius;
            
            // Рисуем дугу
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
            
            // Информация
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 1.2f, 
                $"MEMORY: {rememberedRotation:F1}°\nTime: {turnMemoryTimer:F2}s");
            #endif
        }
    }
}