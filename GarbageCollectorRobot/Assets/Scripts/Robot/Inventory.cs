using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
   public Types.GType garbage = Types.GType.None;

    public void setCell(Types.GType garbage)
    {
        this.garbage = garbage;
    } 

    public Types.GType getCell()
    {
        return garbage;
    }
}
