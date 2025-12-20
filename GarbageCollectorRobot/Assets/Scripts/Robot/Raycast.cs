using UnityEngine;

public class Raycast2DExample : MonoBehaviour
{
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Vector2 rayDirection = Vector2.right;
    
    void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,    
            rayDirection,         
            rayLength,             
            targetLayers           
        );
        
        if (hit.collider != null)
        {
            Debug.Log($"Обнаружен объект: {hit.collider.name}");
            Debug.Log($"Расстояние: {hit.distance}");
            Debug.Log($"Точка попадания: {hit.point}");
            Debug.Log($"Нормаль: {hit.normal}");
            
            if (hit.collider.TryGetComponent<Rigidbody2D>(out var rb))
            {
            }
        }
        
        Debug.DrawRay(transform.position, rayDirection * rayLength, Color.red);
    }
}