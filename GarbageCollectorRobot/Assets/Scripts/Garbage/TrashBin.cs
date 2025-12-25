using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashBin : MonoBehaviour
{
    public Types.GType garbageType = Types.GType.Blue;


    [Header("Спавн объекта")]
    public GameObject spawnPrefab;
    public Vector2 spawnAreaMin = new Vector2(-1f, -1f);
    public Vector2 spawnAreaMax = new Vector2(1f, 1f);
    public bool spawnAtRandomPosition = true;

    public Vector2 worldSpawnPosition;

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
            spawnPrefab.GetComponent<KeepGarbage>().garbageType = (Types.GType)(int)Random.Range(0, 2.99f);
        }
        else
            spawnPosition = (spawnAreaMin + spawnAreaMax) / 2f;
        worldSpawnPosition += spawnPosition;
        Instantiate(spawnPrefab, worldSpawnPosition, Quaternion.identity);    
        Debug.Log($"Объект создан на позиции: {worldSpawnPosition}");
        worldSpawnPosition = new Vector2(0, 0);
    }
}