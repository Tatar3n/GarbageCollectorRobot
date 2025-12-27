using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepGarbage : MonoBehaviour
{
    public Types.GType garbageType = Types.GType.Blue;
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

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
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void ApplyVisuals()
    {
        if (spriteRenderer == null) return;

        // Цвет мусора выбирается через UI (Red/Blue/Yellow).
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
            if (robotInventory != null && robotInventory.getCell() == Types.GType.None)
            {
                robotInventory.setCell(garbageType);
                Debug.Log($"Установил мусор типа: {garbageType}");
                Destroy(gameObject);
            }
        }
    }

}
