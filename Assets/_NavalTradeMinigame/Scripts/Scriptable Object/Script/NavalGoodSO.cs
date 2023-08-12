using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Inventory", menuName = "Mini Game/Goods", order = 1)]
public class NavalGoodSO : ScriptableObject
{
    public List<Items> itemInventory;
}

[System.Serializable]
public class Items
{
    public int id = -1;
    public string goodName = "Good's Name";
    public int goodBasePrice = 10;
    public Sprite goodImage;

    [Space]
    public int goodSellPrice = 0;
}
