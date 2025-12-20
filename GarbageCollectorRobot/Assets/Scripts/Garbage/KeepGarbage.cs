using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepGarbage : MonoBehaviour
{
    public Types.GType garbageType = Types.GType.Blue;

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
