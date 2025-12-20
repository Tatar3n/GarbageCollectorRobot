using System.Collections.Generic;
using UnityEngine;

public class GarbageManager2D : MonoBehaviour
{
    public static GarbageManager2D Instance;
    
    [Header("Префабы")]
    public GameObject garbagePrefab;
    public GameObject trashbinPrefab;
    public GameObject obstaclePrefab;
    
    [Header("Настройки")]
    public int maxGarbageTypes = 3;
    public Color[] typeColors = new Color[3];
    public Sprite[] garbageSprites;
    public Sprite[] trashbinSprites;
    
    private List<GarbageItem2D> garbageItems = new List<GarbageItem2D>();
    private List<Trashbin2D> trashbins = new List<Trashbin2D>();
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    
    public GameObject GetNearestGarbage(Vector2 position)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var garbage in garbageItems)
        {
            if (!garbage.isCollected && garbage.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(position, garbage.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = garbage.gameObject;
                }
            }
        }
        
        return nearest;
    }
    
    public GameObject GetNearestTrashbin(int type, Vector2 position)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var trashbin in trashbins)
        {
            if (trashbin.type == type && trashbin.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(position, trashbin.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = trashbin.gameObject;
                }
            }
        }
        
        return nearest;
    }
    
    public void AddGarbage(Vector2 position, int type)
    {
        GameObject go = Instantiate(garbagePrefab, position, Quaternion.identity, transform);
        GarbageItem2D garbage = go.GetComponent<GarbageItem2D>();
        garbage.type = Mathf.Clamp(type, 1, maxGarbageTypes);
        
        // Настройка спрайта и цвета
        if (garbageSprites != null && garbageSprites.Length > type - 1)
            garbage.SetSprite(garbageSprites[type - 1]);
        garbage.SetColor(typeColors[Mathf.Clamp(type - 1, 0, typeColors.Length - 1)]);
        
        garbageItems.Add(garbage);
    }
    
    public void AddTrashbin(Vector2 position, int type)
    {
        GameObject go = Instantiate(trashbinPrefab, position, Quaternion.identity, transform);
        Trashbin2D trashbin = go.GetComponent<Trashbin2D>();
        trashbin.type = Mathf.Clamp(type, 1, maxGarbageTypes);
        
        // Настройка спрайта и цвета
        if (trashbinSprites != null && trashbinSprites.Length > type - 1)
            trashbin.SetSprite(trashbinSprites[type - 1]);
        trashbin.SetColor(typeColors[Mathf.Clamp(type - 1, 0, typeColors.Length - 1)]);
        
        trashbins.Add(trashbin);
    }
    
    public void AddObstacle(Vector2 position)
    {
        Instantiate(obstaclePrefab, position, Quaternion.identity, transform);
    }
    
    public void ClearAll()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        garbageItems.Clear();
        trashbins.Clear();
    }
    
    public int GetTotalGarbage()
    {
        int count = 0;
        foreach (var garbage in garbageItems)
        {
            if (garbage.gameObject.activeInHierarchy && !garbage.isCollected)
                count++;
        }
        return count;
    }
    
    public int GetCollectedGarbage()
    {
        int count = 0;
        foreach (var garbage in garbageItems)
        {
            if (garbage.isCollected)
                count++;
        }
        return count;
    }
    
    public List<Trashbin2D> GetTrashbins() => new List<Trashbin2D>(trashbins);
    public List<GarbageItem2D> GetGarbageItems() => new List<GarbageItem2D>(garbageItems);
}