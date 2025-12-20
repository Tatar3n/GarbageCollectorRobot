using UnityEngine;

public class RobotSensors2D : MonoBehaviour
{
    [Header("Настройки сенсоров")]
    public float range = 5f;
    public LayerMask obstacleMask;
    public float[] sensorAngles = { -30f, 0f, 30f }; // Левый, центр, правый
    
    [Header("Визуализация")]
    public bool showRays = true;
    public Color rayColor = Color.yellow;
    public Color hitColor = Color.red;
    
    private float[] lastDistances = new float[3];
    private Vector2[] hitPoints = new Vector2[3];
    
    public float[] GetDistances()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 sensorDir = Quaternion.Euler(0, 0, sensorAngles[i]) * transform.up;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, sensorDir, range, obstacleMask);
            
            if (hit.collider != null)
            {
                lastDistances[i] = hit.distance;
                hitPoints[i] = hit.point;
            }
            else
            {
                lastDistances[i] = range;
                hitPoints[i] = transform.position + (Vector3)(sensorDir * range);
            }
        }
        
        return lastDistances;
    }
    
    void Update()
    {
        // Визуализация лучей
        if (showRays)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 sensorDir = Quaternion.Euler(0, 0, sensorAngles[i]) * transform.up;
                Color color = lastDistances[i] < range ? hitColor : rayColor;
                
                Debug.DrawRay(transform.position, sensorDir * Mathf.Min(lastDistances[i], range), color);
                
                // Точка попадания
                if (lastDistances[i] < range)
                {
                    Debug.DrawLine(hitPoints[i] - Vector2.up * 0.1f, 
                                  hitPoints[i] + Vector2.up * 0.1f, hitColor);
                    Debug.DrawLine(hitPoints[i] - Vector2.right * 0.1f, 
                                  hitPoints[i] + Vector2.right * 0.1f, hitColor);
                }
            }
        }
    }
    
    public float GetLeftDistance() => lastDistances[0];
    public float GetCenterDistance() => lastDistances[1];
    public float GetRightDistance() => lastDistances[2];
}