using UnityEngine;
using System.Collections.Generic;

namespace Fuzzy
{
    public class FuzzyLogicSystem : MonoBehaviour
    {
        [Header("Sensors")]
        public Transform frontSensor;
        public Transform back1Sensor;
        public Transform back2Sensor;
        
        [Header("Movement Settings")]
        public float speed = 3f;
        public float detectionRadius = 5f;
        public float obstacleAvoidDistance = 1.5f;
        
        [Header("Smoothing")]
        public float directionSmoothing = 10f;
        public float speedSmoothing = 8f;
        public float maxAcceleration = 20f;

        public LayerMask obstacleLayer;
        public bool showDebug = true;

        private readonly FuzzyFunction fuzzyFunction = new FuzzyFunction();
        private Rigidbody2D rb;
        private Vector2 movementDirection = Vector2.zero;

        private Vector2 smoothedMoveDirection = Vector2.zero;
        private float targetSpeed = 0f;
        private Vector2 desiredVelocity = Vector2.zero;

        void Start()
        {
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


            smoothedMoveDirection = Vector2.right;
            targetSpeed = speed;
        }

        void Update()
        {

            CalculateMovement();
            ApplyRotation();
        }

        void FixedUpdate()
        {

            Vector2 moveDirForPhysics = smoothedMoveDirection;

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
                rb.velocity = Vector2.MoveTowards(rb.velocity, desiredVelocity, maxAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                transform.position += (Vector3)(desiredVelocity * Time.fixedDeltaTime);
            }
        }

        void CalculateMovement()
        {
            // НЕЧЁТКАЯ ЛОГИКА ДЛЯ СКОРОСТИ (по переднему датчику)
            float frontDist = CheckObstacleDistance(frontSensor);
            targetSpeed = fuzzyFunction.Sentr_mass(frontDist);

            // Нечёткая логика для поворота (по боковым датчикам)
            float dRight = back1Sensor ? CheckObstacleDistance(back1Sensor) : float.MaxValue;
            float dLeft = back2Sensor ? CheckObstacleDistance(back2Sensor) : float.MaxValue;
            float dMin =0.0f;
            bool isLeft =true;

            if (Mathf.Abs(dLeft - dRight)>0.5f){
                dMin = Mathf.Min(dLeft, dRight);
                isLeft = dLeft <= dRight;
            }
            else
            {
                dMin = Mathf.Min(dLeft, dRight);
                isLeft = true;
            }

            float turnAngle = fuzzyFunction.Sentr_mass_rotate(dMin, isLeft);
            Debug.Log(dMin);
            Debug.Log(turnAngle);

            Vector2 forward = (smoothedMoveDirection.sqrMagnitude > 0.0001f ? smoothedMoveDirection : Vector2.right).normalized;
            Vector2 turned = (Vector2)(Quaternion.Euler(0f, 0f, turnAngle) * forward);
            
            if (turned.sqrMagnitude > 0.0001f)
            {
                movementDirection = turned.normalized;
            }
            else
            {
                movementDirection = forward;
            }
        }

        void SmoothMovementAndSpeed()
        {
            float dt = Time.deltaTime;
            float dirAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * dt);
            float spdAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, speedSmoothing) * dt);

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, movementDirection.normalized, dirAlpha);
            }
            else
            {
                smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, Vector2.zero, dirAlpha);
            }

            speed = Mathf.Lerp(speed, targetSpeed, spdAlpha);
            speed = Mathf.Max(0f, speed);
        }

        void ApplyRotation()
        {
            if (smoothedMoveDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(smoothedMoveDirection.y, smoothedMoveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        Vector2 GetSensorWorldDirection(Transform sensor)
        {
            if (sensor == null) return Vector2.zero;
            Vector2 fromCenter = (Vector2)sensor.position - (Vector2)transform.position;
            if (fromCenter.sqrMagnitude < 0.0001f) return Vector2.zero;
            return fromCenter.normalized;
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
    }
}