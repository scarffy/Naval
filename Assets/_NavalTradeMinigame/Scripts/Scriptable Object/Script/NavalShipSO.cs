using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Ship", menuName = "Mini Game/Ship", order = 3)]
public class NavalShipSO : ScriptableObject
{
    [Header("Basic Information")]
    public int shipId = -1;
    public string shipName;
    public int shipHP;
    public int shipPrice;
    public Sprite shipImage;

    public float shipSpeed;

    [Header("Inventory")]
    public int maximumInventory = 0;
}
