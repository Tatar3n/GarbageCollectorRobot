using UnityEngine;

public class Trashbin2D : MonoBehaviour
{
    public int type = 1;
    public int garbageCount = 0;
    public int maxCapacity = 10;
    
    private SpriteRenderer spriteRenderer;
    private TextMesh countText;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        countText = GetComponentInChildren<TextMesh>();
        UpdateDisplay();
    }
    
    public void ReceiveGarbage()
    {
        if (garbageCount < maxCapacity)
        {
            garbageCount++;
            UpdateDisplay();
            
            // Визуальный эффект
            StartCoroutine(PulseEffect());
        }
    }
    
    void UpdateDisplay()
    {
        if (countText != null)
            countText.text = $"{garbageCount}/{maxCapacity}";
            
        // Изменение прозрачности при заполнении
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.5f + (garbageCount / (float)maxCapacity) * 0.5f;
            spriteRenderer.color = color;
        }
    }
    
    System.Collections.IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.2f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = originalScale;
    }
    
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }
    
    public void SetColor(Color color)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }
}