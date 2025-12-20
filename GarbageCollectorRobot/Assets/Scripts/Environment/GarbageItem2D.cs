using UnityEngine;

public class GarbageItem2D : MonoBehaviour
{
    public int type = 1;
    public bool isCollected = false;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D col2D;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col2D = GetComponent<Collider2D>();
    }
    
    public void Collect()
    {
        isCollected = true;
        spriteRenderer.enabled = false;
        if (col2D != null) col2D.enabled = false;
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