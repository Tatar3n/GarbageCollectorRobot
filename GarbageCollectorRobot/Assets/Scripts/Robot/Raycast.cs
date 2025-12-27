using UnityEngine;

public class Raycast2DExample : MonoBehaviour
{
    [SerializeField] private float rayLength = 1f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Vector2 rayDirection = Vector2.right;
    
    void Update()
    {
        // ВАЖНО: rayDirection хранится в локальных координатах сенсора.
        // Иначе при повороте робота отладочный луч будет "залипать" в мировом направлении,
        // создавая впечатление, что рейкасты не поворачиваются.
        Vector2 worldDir = (Vector2)transform.TransformDirection(rayDirection.normalized);
        if (worldDir.sqrMagnitude < 0.0001f) worldDir = (Vector2)transform.right;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,    
            worldDir,
            rayLength,
            targetLayers
        );
        
        if (hit.collider != null)
        {
            //Debug.Log($"Обнаружен объект: {hit.collider.tag}");
            //Debug.Log($"Расстояние: {hit.distance}");
            //Debug.Log($"Точка попадания: {hit.point}");
            //Debug.Log($"Нормаль: {hit.normal}");
            
            
            if (hit.collider.TryGetComponent<Rigidbody2D>(out var rb))
            {
            }
        }
        
        Debug.DrawRay(transform.position, worldDir * rayLength, Color.red);
    }
}