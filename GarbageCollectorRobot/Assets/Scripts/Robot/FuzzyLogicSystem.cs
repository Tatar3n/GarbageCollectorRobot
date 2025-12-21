using UnityEngine;
using System.Collections.Generic;

public class FuzzyLogicSystem : MonoBehaviour
{
    // ========== НАСТРОЙКИ ==========
    [Header("Настройки движения")]
    public float speed = 3f;
    public float detectionRadius = 5f;
    public float obstacleAvoidDistance = 1.5f;

    [Header("Слои")]
    public LayerMask obstacleLayer;
    public LayerMask garbageLayer;

    [Header("4 сенсора избегания")]
    public Transform frontSensor;
    public Transform backSensor;
    public Transform leftSensor;
    public Transform rightSensor;

    [Header("Отладка")]
    public bool showDebug = true;

    // ========== ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ==========
    private Inventory inventory;
    private Dictionary<Types.GType, Transform> trashbins = new Dictionary<Types.GType, Transform>();

    private Vector2 movementDirection = Vector2.zero;

    // Цели
    private Transform currentTarget = null;
    private Types.GType currentGarbageType = Types.GType.None;

    // Поиск
    private Vector2 searchDirection = Vector2.right;
    private float searchChangeTime = 2f;
    private float searchTimer = 0f;

    // Состояния
    private enum RobotState { Searching, GoingToGarbage, GoingToTrashbin, Unloading }
    private RobotState currentState = RobotState.Searching;

    void Start()
    {
        // Получаем инвентарь
        inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Нет компонента Inventory на роботе!");
            enabled = false;
            return;
        }

        // Находим все мусорки
        FindAllTrashbins();

        // Начальное направление
        searchDirection = Random.insideUnitCircle.normalized;

        Debug.Log("Робот-сборщик запущен. Мусорок: " + trashbins.Count);
    }

    void Update()
    {
        // 1. Обновляем состояние на основе инвентаря
        UpdateRobotState();

        // 2. Выбираем цель в зависимости от состояния
        SelectTargetBasedOnState();

        // 3. Рассчитываем движение с учетом препятствий
        CalculateMovement();

        // 4. Двигаемся
        Move();

        // 5. Отладка
        if (showDebug) DebugInfo();
    }

    // ========== ОСНОВНЫЕ ФУНКЦИИ ==========

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
        // Получаем текущий тип мусора в инвентаре
        Types.GType inventoryGarbage = inventory.getCell();

        // Если инвентарь изменился
        if (inventoryGarbage != currentGarbageType)
        {
            currentGarbageType = inventoryGarbage;

            // Определяем новое состояние
            if (inventoryGarbage == Types.GType.None)
            {
                // Инвентарь пуст - начинаем поиск
                currentState = RobotState.Searching;
                currentTarget = null;
                Debug.Log("Инвентарь пуст - начинаю поиск мусора");
            }
            else
            {
                // Подобрали мусор - едем к мусорке
                currentState = RobotState.GoingToTrashbin;
                Debug.Log($"Подобрал мусор типа: {inventoryGarbage} - еду к мусорке");
            }
        }

        // Если мы в состоянии разгрузки и достигли мусорки
        if (currentState == RobotState.GoingToTrashbin && currentTarget != null)
        {
            float distance = Vector2.Distance(transform.position, currentTarget.position);
            if (distance < 0.5f)
            {
                currentState = RobotState.Unloading;
                Debug.Log("Достиг мусорки - разгружаюсь");
            }
        }
    }

    void SelectTargetBasedOnState()
    {
        switch (currentState)
        {
            case RobotState.Searching:
                // Ищем ближайший мусор
                FindNearestGarbage();
                if (currentTarget != null)
                {
                    currentState = RobotState.GoingToGarbage;
                }
                break;

            case RobotState.GoingToGarbage:
                // Цель уже установлена (мусор)
                // Проверяем не подобран ли уже
                if (inventory.getCell() != Types.GType.None)
                {
                    currentState = RobotState.GoingToTrashbin;
                    currentTarget = null;
                }
                break;

            case RobotState.GoingToTrashbin:
                // Находим мусорку для текущего типа мусора
                if (currentTarget == null && trashbins.ContainsKey(currentGarbageType))
                {
                    currentTarget = trashbins[currentGarbageType];
                }
                break;

            case RobotState.Unloading:
                // Ждем пока TrashBin.OnTriggerEnter2D очистит инвентарь
                // После очистки UpdateRobotState переведет в Searching
                break;
        }
    }

    void FindNearestGarbage()
    {
        // Сбрасываем цель
        currentTarget = null;

        // Ищем мусор в радиусе
        Collider2D[] garbageColliders = Physics2D.OverlapCircleAll(
            transform.position,
            detectionRadius,
            garbageLayer
        );

        if (garbageColliders.Length > 0)
        {
            // Берем самый близкий
            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var collider in garbageColliders)
            {
                // Проверяем что объект еще существует
                if (collider == null) continue;

                float dist = Vector2.Distance(transform.position, collider.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = collider.transform;
                }
            }

            currentTarget = closest;
            if (currentTarget != null)
            {
                Debug.Log($"Нашел мусор на расстоянии: {minDist:F1}");
            }
        }
    }

    void CalculateMovement()
    {
        Vector2 targetDirection = Vector2.zero;

        // Если есть цель - идем к ней
        if (currentTarget != null && currentState != RobotState.Unloading)
        {
            Vector2 toTarget = (Vector2)currentTarget.position - (Vector2)transform.position;

            // Если очень близко к цели - останавливаемся
            float distance = toTarget.magnitude;
            if (distance < 0.3f)
            {
                movementDirection = Vector2.zero;
                return;
            }

            targetDirection = toTarget.normalized;
        }
        // Если нет цели (патрулируем) - случайное движение
        else if (currentState == RobotState.Searching)
        {
            // Меняем направление поиска периодически
            searchTimer += Time.deltaTime;
            if (searchTimer > searchChangeTime)
            {
                searchDirection = Random.insideUnitCircle.normalized;
                searchTimer = 0f;
                searchChangeTime = Random.Range(1f, 3f);
                Debug.Log("Меняю направление поиска");
            }

            targetDirection = searchDirection;
        }
        // Если разгружаемся - стоим на месте
        else if (currentState == RobotState.Unloading)
        {
            movementDirection = Vector2.zero;
            return;
        }

        // ПРОВЕРКА ПРЕПЯТСТВИЙ
        Vector2 avoidDirection = Vector2.zero;

        // Проверяем все 4 направления
        if (frontSensor != null)
        {
            float frontDist = CheckObstacleDistance(frontSensor, Vector2.up);
            if (frontDist < obstacleAvoidDistance)
            {
                avoidDirection += Vector2.down * (1f - (frontDist / obstacleAvoidDistance));
            }
        }

        if (backSensor != null)
        {
            float backDist = CheckObstacleDistance(backSensor, Vector2.down);
            if (backDist < obstacleAvoidDistance)
            {
                avoidDirection += Vector2.up * (1f - (backDist / obstacleAvoidDistance));
            }
        }

        if (leftSensor != null)
        {
            float leftDist = CheckObstacleDistance(leftSensor, Vector2.left);
            if (leftDist < obstacleAvoidDistance)
            {
                avoidDirection += Vector2.right * (1f - (leftDist / obstacleAvoidDistance));
            }
        }

        if (rightSensor != null)
        {
            float rightDist = CheckObstacleDistance(rightSensor, Vector2.right);
            if (rightDist < obstacleAvoidDistance)
            {
                avoidDirection += Vector2.left * (1f - (rightDist / obstacleAvoidDistance));
            }
        }

        // Комбинируем направление к цели и избегание препятствий
        if (avoidDirection.magnitude > 0.1f)
        {
            // Приоритет: избегание препятствий
            movementDirection = (targetDirection + avoidDirection * 2f).normalized;
        }
        else
        {
            movementDirection = targetDirection;
        }
    }

    float CheckObstacleDistance(Transform sensor, Vector2 direction)
    {
        if (sensor == null) return float.MaxValue;

        RaycastHit2D hit = Physics2D.Raycast(
            sensor.position,
            direction,
            obstacleAvoidDistance * 1.5f,
            obstacleLayer
        );

        // Рисуем луч для отладки
        if (showDebug)
        {
            Debug.DrawRay(sensor.position, direction * obstacleAvoidDistance * 1.5f,
                hit.collider ? Color.red : Color.green);
        }

        return hit.collider ? hit.distance : float.MaxValue;
    }

    void Move()
    {
        // Применяем движение
        if (movementDirection.magnitude > 0.1f)
        {
            transform.position += (Vector3)movementDirection * speed * Time.deltaTime;
        }
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ==========

    void DebugInfo()
    {
        string stateStr = "";
        switch (currentState)
        {
            case RobotState.Searching: stateStr = "Поиск мусора"; break;
            case RobotState.GoingToGarbage: stateStr = "Еду к мусору"; break;
            case RobotState.GoingToTrashbin: stateStr = "Еду к мусорке"; break;
            case RobotState.Unloading: stateStr = "Разгружаюсь"; break;
        }

        string targetStr = currentTarget != null ?
            (currentState == RobotState.GoingToTrashbin ? "Мусорка" : "Мусор") : "Нет цели";

        string info = $"Состояние: {stateStr} | Инвентарь: {currentGarbageType} | Цель: {targetStr}";

        // Выводим в консоль
        Debug.Log(info);
    }

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Направление движения
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, movementDirection * 1f);

        // Цель
        if (currentTarget != null)
        {
            Gizmos.color = (currentState == RobotState.GoingToTrashbin) ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.3f);
        }

        // Зона избегания препятствий
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, obstacleAvoidDistance);
    }
}