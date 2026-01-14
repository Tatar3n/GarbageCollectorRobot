using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashBin : MonoBehaviour
{
    public Types.GType garbageType = Types.GType.Blue;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Спавн объекта")]
    public GameObject spawnPrefab;
    public Vector2 spawnAreaMin = new Vector2(-1f, -1f);
    public Vector2 spawnAreaMax = new Vector2(1f, 1f);
    public bool spawnAtRandomPosition = true;

    public Vector2 worldSpawnPosition;

    private void Awake()
    {
        EnsureSpriteRenderer();
        ApplyVisuals();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSpriteRenderer();
        ApplyVisuals();
    }
#endif

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ApplyVisuals()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = garbageType switch
        {
            Types.GType.Red => new Color(1f, 0.25f, 0.25f, 1f),
            Types.GType.Blue => new Color(0.25f, 0.55f, 1f, 1f),
            Types.GType.Yellow => new Color(1f, 0.9f, 0.2f, 1f),
            _ => Color.white
        };
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("robot"))
        {
            Inventory robotInventory = collision.GetComponentInParent<Inventory>();     
            if (robotInventory != null && robotInventory.getCell() == garbageType)
            {
                robotInventory.setCell(Types.GType.None);
                Debug.Log($"Удалил мусор типа: {garbageType}");
                SpawnObject();
            }
        }
    }
    
    void SpawnObject()
    {
        if (spawnPrefab == null)
        {
            Debug.LogWarning("Префаб для спавна не установлен!");
            return;
        }    
        Vector2 spawnPosition;       
        if (spawnAtRandomPosition)
        {
            float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float randomY = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            spawnPosition = new Vector2(randomX, randomY);
        }
        else
            spawnPosition = (spawnAreaMin + spawnAreaMax) / 2f;
        worldSpawnPosition += spawnPosition;
        GameObject go = Instantiate(spawnPrefab, worldSpawnPosition, Quaternion.identity);
        if (spawnAtRandomPosition && go.TryGetComponent<KeepGarbage>(out var keep))
        {
            keep.garbageType = (Types.GType)(int)Random.Range(0, 2.99f);
            keep.ApplyVisuals();
        }
        Debug.Log($"Объект создан на позиции: {worldSpawnPosition}");
        worldSpawnPosition = new Vector2(0, 0);
    }
}