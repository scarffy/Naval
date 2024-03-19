using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Item", menuName = "Mini Game/Item", order = 2)]
public class NavalItem : ScriptableObject
{
    public int id = -1;
    public string goodName = "Good's Name";
    public int goodBasePrice = 10;
    public Sprite goodImage;

    [Space]
    public int goodSellPrice = 0;
}
