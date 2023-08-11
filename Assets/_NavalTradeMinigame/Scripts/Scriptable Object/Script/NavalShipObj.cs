using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Good", menuName = "Game Jam/Ship", order = 1)]
public class NavalShipObj : ScriptableObject
{
    public string shipName;
    public string shipPrice;
    public Sprite shipImage;

    public float shipSpeed;

    // Inventory
}
