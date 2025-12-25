using UnityEngine;

public class DeathObject : MonoBehaviour
{
    [Header("Настройки смертельного объекта")]
    [Tooltip("Если true, робот будет уничтожен мгновенно при касании")]
    public bool destroyImmediately = true;

    [Tooltip("Если destroyImmediately = false, задержка перед уничтожением")]
    public float destroyDelay = 0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Robot"))
        {
            HandleRobotDeath(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("robot"))
        {
            HandleRobotDeath(collision.gameObject);
        }
    }

    private void HandleRobotDeath(GameObject robot)
    {
    
        if (destroyImmediately)
        {
            Destroy(robot);
        }
        else
        {
            Destroy(robot, destroyDelay);
        }

        Debug.Log($"Робот {robot.name} был уничтожен объектом {gameObject.name}");
    }
}