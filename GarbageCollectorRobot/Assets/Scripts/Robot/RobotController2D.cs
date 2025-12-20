using System.Collections;
using UnityEngine;
using FuzzyLogic2D;

public class RobotController2D : MonoBehaviour
{
    [Header("Настройки движения")]
    public float maxSpeed = 5f;
    public float turnSpeed = 180f;
    public float sensorRange = 5f;
    public float pickupRange = 0.5f;
    public float dumpRange = 0.7f;
    
    [Header("Компоненты")]
    public FuzzySystem2D fuzzySystem;
    public RobotSensors2D sensors;
    public GarbageManager2D garbageManager;
    
    [Header("Спрайты")]
    public SpriteRenderer robotSprite;
    public Sprite normalSprite;
    public Sprite carryingSprite;
    public GameObject sensorVisualizer;
    
    [Header("Состояние")]
    public int carryingGarbageType = 0; // 0 = пустой
    public int collectedCount = 0;
    public int totalGarbage = 0;
    public bool isMissionComplete = false;
    
    private Rigidbody2D rb;
    private float currentSpeed = 0f;
    private float currentTurn = 0f;
    private Coroutine currentActionCoroutine;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sensors = GetComponent<RobotSensors2D>();
        garbageManager = FindObjectOfType<GarbageManager2D>();
        
        if (garbageManager != null)
            totalGarbage = garbageManager.GetTotalGarbage();
            
        UpdateSprite();
    }
    
    void Update()
    {
        if (isMissionComplete || Time.timeScale < 0.1f) return;
        
        // Получение данных сенсоров
        float[] sensorDistances = sensors.GetDistances();
        
        // Определение цели
        GameObject target = GetCurrentTarget();
        float targetAngle = 0f;
        float targetDistance = float.MaxValue;
        bool isGarbageTarget = false;
        
        if (target != null)
        {
            Vector2 toTarget = target.transform.position - transform.position;
            targetDistance = toTarget.magnitude;
            
            // Угол между направлением робота и целью
            Vector2 forward = transform.up;
            targetAngle = Vector2.SignedAngle(forward, toTarget) * Mathf.Deg2Rad;
            
            isGarbageTarget = target.CompareTag("Garbage");
        }
        
        // Обработка нечеткой логикой
        var fuzzyOutputs = fuzzySystem.Process(
            sensorDistances,
            targetAngle,
            targetDistance,
            isGarbageTarget,
            Time.deltaTime
        );
        
        // Применение выходов
        currentSpeed = fuzzyOutputs["speed"] * maxSpeed;
        currentTurn = fuzzyOutputs["turn"] * turnSpeed * Mathf.Deg2Rad;
        
        // Обработка действий
        ProcessActions(fuzzyOutputs["action"], target);
        
        // Обновление состояния системы нечеткой логики
        fuzzySystem.carryingType = carryingGarbageType;
        
        // Проверка завершения миссии
        if (collectedCount >= totalGarbage && totalGarbage > 0)
        {
            isMissionComplete = true;
            currentSpeed = 0f;
            Debug.Log("Миссия выполнена! Весь мусор собран.");
        }
    }
    
    void FixedUpdate()
    {
        if (isMissionComplete) return;
        
        // Применение поворота
        float turnAmount = currentTurn * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation + turnAmount * Mathf.Rad2Deg);
        
        // Применение движения
        Vector2 moveDirection = transform.up * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveDirection);
    }
    
    GameObject GetCurrentTarget()
    {
        if (carryingGarbageType > 0)
        {
            // Ищем мусорку соответствующего типа
            return garbageManager.GetNearestTrashbin(carryingGarbageType, transform.position);
        }
        else
        {
            // Ищем ближайший не собранный мусор
            return garbageManager.GetNearestGarbage(transform.position);
        }
    }
    
    void ProcessActions(float actionValue, GameObject target)
    {
        if (actionValue > 0.7f && target != null && currentActionCoroutine == null)
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);
            
            if (target.CompareTag("Garbage") && distance < pickupRange)
            {
                currentActionCoroutine = StartCoroutine(PickupGarbageRoutine(target));
            }
            else if (target.CompareTag("Trashbin") && distance < dumpRange && carryingGarbageType > 0)
            {
                currentActionCoroutine = StartCoroutine(DumpGarbageRoutine(target));
            }
        }
    }
    
    IEnumerator PickupGarbageRoutine(GameObject garbageObj)
    {
        GarbageItem2D garbage = garbageObj.GetComponent<GarbageItem2D>();
        if (garbage != null && !garbage.isCollected)
        {
            // Остановка на время подбора
            float originalSpeed = currentSpeed;
            currentSpeed = 0f;
            
            // Анимация подбора
            yield return new WaitForSeconds(0.5f);
            
            carryingGarbageType = garbage.type;
            garbage.Collect();
            collectedCount++;
            fuzzySystem.trashLevel = Mathf.Min(1f, fuzzySystem.trashLevel + 0.1f);
            fuzzySystem.timeSinceLastAction = 0f;
            
            UpdateSprite();
            
            Debug.Log($"Подобран мусор типа {carryingGarbageType}. Всего собрано: {collectedCount}/{totalGarbage}");
            
            currentSpeed = originalSpeed;
        }
        
        currentActionCoroutine = null;
    }
    
    IEnumerator DumpGarbageRoutine(GameObject trashbinObj)
    {
        Trashbin2D trashbin = trashbinObj.GetComponent<Trashbin2D>();
        if (trashbin != null && trashbin.type == carryingGarbageType)
        {
            // Остановка на время выгрузки
            float originalSpeed = currentSpeed;
            currentSpeed = 0f;
            
            // Анимация выгрузки
            yield return new WaitForSeconds(1f);
            
            trashbin.ReceiveGarbage();
            carryingGarbageType = 0;
            fuzzySystem.trashLevel = 0f;
            fuzzySystem.timeSinceLastAction = 0f;
            
            UpdateSprite();
            
            Debug.Log($"Мусор выгружен в мусорку типа {trashbin.type}");
            
            currentSpeed = originalSpeed;
        }
        
        currentActionCoroutine = null;
    }
    
    void UpdateSprite()
    {
        if (robotSprite != null)
        {
            robotSprite.sprite = carryingGarbageType > 0 ? carryingSprite : normalSprite;
            
            // Изменение цвета в зависимости от типа мусора
            if (carryingGarbageType > 0)
            {
                Color[] typeColors = { Color.white, Color.red, Color.green, Color.blue };
                robotSprite.color = typeColors[Mathf.Clamp(carryingGarbageType, 0, 3)];
            }
            else
            {
                robotSprite.color = Color.white;
            }
        }
    }
    
    public void ResetRobot()
    {
        if (currentActionCoroutine != null)
            StopCoroutine(currentActionCoroutine);
            
        carryingGarbageType = 0;
        collectedCount = 0;
        currentSpeed = 0f;
        currentTurn = 0f;
        isMissionComplete = false;
        
        if (fuzzySystem != null)
        {
            fuzzySystem.timeSinceLastAction = 0f;
            fuzzySystem.trashLevel = 0f;
        }
        
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        
        UpdateSprite();
    }
    
    void OnDrawGizmos()
    {
        // Визуализация направления
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 1f);
        
        // Визуализация цели
        GameObject target = GetCurrentTarget();
        if (target != null)
        {
            Gizmos.color = carryingGarbageType > 0 ? Color.red : Color.blue;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
        
        // Визуализация диапазона подбора
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}