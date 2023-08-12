using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Ship", menuName = "Mini Game/Ship", order = 1)]
public class NavalShipSO : ScriptableObject
{
    [Header("Basic Information")]
    public int shipId = -1;
    public string shipName;
    public int shipHP;
    public string shipPrice;
    public Sprite shipImage;

    public float shipSpeed;

    [Header("Inventory")]
    public int maximumInventory = 0;
    [Space]
    public List<Items> shipInventory;
}
