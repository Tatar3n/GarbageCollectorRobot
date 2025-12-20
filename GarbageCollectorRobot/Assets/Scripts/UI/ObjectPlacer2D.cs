using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectPlacer2D : MonoBehaviour
{
    public enum PlacementMode { Obstacle, Garbage, Trashbin }
    
    [Header("Настройки")]
    public PlacementMode currentMode = PlacementMode.Obstacle;
    public int currentGarbageType = 1;
    public int currentTrashbinType = 1;
    
    [Header("Превью")]
    public GameObject previewObject;
    public Sprite obstaclePreviewSprite;
    public Sprite[] garbagePreviewSprites;
    public Sprite[] trashbinPreviewSprites;
    
    [Header("Цвета")]
    public Color obstacleColor = Color.gray;
    public Color[] garbageColors = { Color.red, Color.green, Color.blue };
    
    private Camera mainCamera;
    private SpriteRenderer previewRenderer;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Создаем объект превью
        previewObject = new GameObject("Preview");
        previewRenderer = previewObject.AddComponent<SpriteRenderer>();
        previewRenderer.sortingOrder = 100;
        previewRenderer.color = new Color(1, 1, 1, 0.7f);
    }
    
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        // Получение позиции мыши в мире
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        UpdatePreview(mouseWorldPos);
        
        // Размещение объекта
        if (Input.GetMouseButtonDown(0))
        {
            PlaceObject(mouseWorldPos);
        }
        
        // Удаление объекта
        if (Input.GetMouseButtonDown(1))
        {
            RemoveObject(mouseWorldPos);
        }
    }
    
    void UpdatePreview(Vector3 position)
    {
        if (previewObject == null) return;
        
        previewObject.transform.position = position;
        
        // Настройка спрайта превью
        switch (currentMode)
        {
            case PlacementMode.Obstacle:
                previewRenderer.sprite = obstaclePreviewSprite;
                previewRenderer.color = new Color(obstacleColor.r, obstacleColor.g, obstacleColor.b, 0.7f);
                previewObject.transform.localScale = Vector3.one;
                break;
                
            case PlacementMode.Garbage:
                if (garbagePreviewSprites != null && garbagePreviewSprites.Length >= currentGarbageType)
                    previewRenderer.sprite = garbagePreviewSprites[currentGarbageType - 1];
                Color garbageColor = garbageColors[Mathf.Clamp(currentGarbageType - 1, 0, garbageColors.Length - 1)];
                previewRenderer.color = new Color(garbageColor.r, garbageColor.g, garbageColor.b, 0.7f);
                previewObject.transform.localScale = Vector3.one * 0.8f;
                break;
                
            case PlacementMode.Trashbin:
                if (trashbinPreviewSprites != null && trashbinPreviewSprites.Length >= currentTrashbinType)
                    previewRenderer.sprite = trashbinPreviewSprites[currentTrashbinType - 1];
                Color trashbinColor = garbageColors[Mathf.Clamp(currentTrashbinType - 1, 0, garbageColors.Length - 1)];
                previewRenderer.color = new Color(trashbinColor.r, trashbinColor.g, trashbinColor.b, 0.7f);
                previewObject.transform.localScale = Vector3.one * 1.2f;
                break;
        }
    }
    
    void PlaceObject(Vector3 position)
    {
        switch (currentMode)
        {
            case PlacementMode.Obstacle:
                GarbageManager2D.Instance.AddObstacle(position);
                break;
            case PlacementMode.Garbage:
                GarbageManager2D.Instance.AddGarbage(position, currentGarbageType);
                break;
            case PlacementMode.Trashbin:
                GarbageManager2D.Instance.AddTrashbin(position, currentTrashbinType);
                break;
        }
    }
    
    void RemoveObject(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.5f);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Garbage") || col.CompareTag("Trashbin") || col.CompareTag("Obstacle"))
            {
                Destroy(col.gameObject);
                break;
            }
        }
    }
    
    public void SetModeObstacle() => currentMode = PlacementMode.Obstacle;
    public void SetModeGarbage() => currentMode = PlacementMode.Garbage;
    public void SetModeTrashbin() => currentMode = PlacementMode.Trashbin;
    
    public void SetGarbageType(int type) => currentGarbageType = Mathf.Clamp(type, 1, 3);
    public void SetTrashbinType(int type) => currentTrashbinType = Mathf.Clamp(type, 1, 3);
    
    void OnDestroy()
    {
        if (previewObject != null)
            Destroy(previewObject);
    }
}