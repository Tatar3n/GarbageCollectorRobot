using UnityEngine;
using System.Collections.Generic;
namespace Fuzzy
{
public class FuzzyLogicSystem : MonoBehaviour
{
    // ========== ��������� ==========
    [Header("��������� ��������")]
    public float speed = 3f;
    public float detectionRadius = 5f;
    public float obstacleAvoidDistance = 1.5f;

    [Header("����")]
    public LayerMask obstacleLayer;
    public LayerMask garbageLayer;
    // Создаем экземпляр FuzzyFunction
    private FuzzyFunction fuzzyFunction = new FuzzyFunction();

    [Header("4 ������� ���������")]
    public Transform frontSensor;
    public Transform backSensor;
    public Transform leftSensor;
    public Transform rightSensor;

    [Header("�������")]
    public bool showDebug = true;

    // ========== ���������� ���������� ==========
    private Inventory inventory;
    private Dictionary<Types.GType, Transform> trashbins = new Dictionary<Types.GType, Transform>();

    private Vector2 movementDirection = Vector2.zero;

    // ����
    private Transform currentTarget = null;
    private Types.GType currentGarbageType = Types.GType.None;

    // �����
    private Vector2 searchDirection = Vector2.right;
    private float searchChangeTime = 2f;
    private float searchTimer = 0f;

    // ���������
    private enum RobotState { Searching, GoingToGarbage, GoingToTrashbin, Unloading }
    private RobotState currentState = RobotState.Searching;

    void Start()
    {
        // �������� ���������
        inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("��� ���������� Inventory �� ������!");
            enabled = false;
            return;
        }

        // ������� ��� �������
        FindAllTrashbins();

        // ��������� �����������
        searchDirection = Random.insideUnitCircle.normalized;

        Debug.Log("�����-������� �������. �������: " + trashbins.Count);
    }

    void Update()
    {
        // 1. ��������� ��������� �� ������ ���������
        UpdateRobotState();

        // 2. �������� ���� � ����������� �� ���������
        SelectTargetBasedOnState();

        // 3. ������������ �������� � ������ �����������
        CalculateMovement();

        // 4. ���������
        Move();

        // 5. �������
        if (showDebug) DebugInfo();
    }

    // ========== �������� ������� ==========

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
        // �������� ������� ��� ������ � ���������
        Types.GType inventoryGarbage = inventory.getCell();

        // ���� ��������� ���������
        if (inventoryGarbage != currentGarbageType)
        {
            currentGarbageType = inventoryGarbage;

            // ���������� ����� ���������
            if (inventoryGarbage == Types.GType.None)
            {
                // ��������� ���� - �������� �����
                currentState = RobotState.Searching;
                currentTarget = null;
                Debug.Log("��������� ���� - ������� ����� ������");
            }
            else
            {
                // ��������� ����� - ���� � �������
                currentState = RobotState.GoingToTrashbin;
                Debug.Log($"�������� ����� ����: {inventoryGarbage} - ��� � �������");
            }
        }

        // ���� �� � ��������� ��������� � �������� �������
        if (currentState == RobotState.GoingToTrashbin && currentTarget != null)
        {
            float distance = Vector2.Distance(transform.position, currentTarget.position);
            if (distance < 0.5f)
            {
                currentState = RobotState.Unloading;
                Debug.Log("������ ������� - �����������");
            }
        }
    }

    void SelectTargetBasedOnState()
    {
        switch (currentState)
        {
            case RobotState.Searching:
                // ���� ��������� �����
                FindNearestGarbage();
                if (currentTarget != null)
                {
                    currentState = RobotState.GoingToGarbage;
                }
                break;

            case RobotState.GoingToGarbage:
                // ���� ��� ����������� (�����)
                // ��������� �� �������� �� ���
                if (inventory.getCell() != Types.GType.None)
                {
                    currentState = RobotState.GoingToTrashbin;
                    currentTarget = null;
                }
                break;

            case RobotState.GoingToTrashbin:
                // ������� ������� ��� �������� ���� ������
                if (currentTarget == null && trashbins.ContainsKey(currentGarbageType))
                {
                    currentTarget = trashbins[currentGarbageType];
                }
                break;

            case RobotState.Unloading:
                // ���� ���� TrashBin.OnTriggerEnter2D ������� ���������
                // ����� ������� UpdateRobotState ��������� � Searching
                break;
        }
    }

    void FindNearestGarbage()
    {
        // ���������� ����
        currentTarget = null;

        // ���� ����� � �������
        Collider2D[] garbageColliders = Physics2D.OverlapCircleAll(
            transform.position,
            detectionRadius,
            garbageLayer
        );

        if (garbageColliders.Length > 0)
        {
            // ����� ����� �������
            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var collider in garbageColliders)
            {
                // ��������� ��� ������ ��� ����������
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
                Debug.Log($"����� ����� �� ����������: {minDist:F1}");
            }
        }
    }

    void CalculateMovement()
    {
        Vector2 targetDirection = Vector2.zero;

        // ���� ���� ���� - ���� � ���
        if (currentTarget != null && currentState != RobotState.Unloading)
        {
            Vector2 toTarget = (Vector2)currentTarget.position - (Vector2)transform.position;

            // ���� ����� ������ � ���� - ���������������
            float distance = toTarget.magnitude;
            if (distance < 0.3f)
            {
                movementDirection = Vector2.zero;
                return;
            }

            targetDirection = toTarget.normalized;
        }
        // ���� ��� ���� (�����������) - ��������� ��������
        else if (currentState == RobotState.Searching)
        {
            // ������ ����������� ������ ������������
            searchTimer += Time.deltaTime;
            if (searchTimer > searchChangeTime)
            {
                searchDirection = Random.insideUnitCircle.normalized;
                searchTimer = 0f;
                searchChangeTime = Random.Range(1f, 3f);
                Debug.Log("����� ����������� ������");
            }

            targetDirection = searchDirection;
        }
        // ���� ������������ - ����� �� �����
        else if (currentState == RobotState.Unloading)
        {
            movementDirection = Vector2.zero;
            return;
        }

        // �������� �����������
        Vector2 avoidDirection = Vector2.zero;

        // ��������� ��� 4 �����������
        if (frontSensor != null)
        {
            float frontDist = CheckObstacleDistance(frontSensor, Vector2.up);
            speed = fuzzyFunction.Sentr_mass(frontDist);
            Debug.Log($"Скорость: {speed}");
            Debug.Log($"Дистанция: {frontDist}");
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

        // ����������� ����������� � ���� � ��������� �����������
        if (avoidDirection.magnitude > 0.1f)
        {
            // ���������: ��������� �����������
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

        // ������ ��� ��� �������
        if (showDebug)
        {
            Debug.DrawRay(sensor.position, direction * obstacleAvoidDistance * 1.5f,
                hit.collider ? Color.red : Color.green);
        }

        return hit.collider ? hit.distance : float.MaxValue;
    }

    void Move()
    {
        // ��������� ��������
        if (movementDirection.magnitude > 0.1f)
        {
            transform.position += (Vector3)movementDirection * speed * Time.deltaTime;
        }
    }

    // ========== ��������������� ������� ==========

    void DebugInfo()
    {
        string stateStr = "";
        switch (currentState)
        {
            case RobotState.Searching: stateStr = "����� ������"; break;
            case RobotState.GoingToGarbage: stateStr = "��� � ������"; break;
            case RobotState.GoingToTrashbin: stateStr = "��� � �������"; break;
            case RobotState.Unloading: stateStr = "�����������"; break;
        }

        string targetStr = currentTarget != null ?
            (currentState == RobotState.GoingToTrashbin ? "�������" : "�����") : "��� ����";

        string info = $"���������: {stateStr} | ���������: {currentGarbageType} | ����: {targetStr}";

        // ������� � �������
        Debug.Log(info);
    }

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        // ������ �����������
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // ����������� ��������
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, movementDirection * 1f);

        // ����
        if (currentTarget != null)
        {
            Gizmos.color = (currentState == RobotState.GoingToTrashbin) ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.3f);
        }

        // ���� ��������� �����������
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, obstacleAvoidDistance);
    }
}
}