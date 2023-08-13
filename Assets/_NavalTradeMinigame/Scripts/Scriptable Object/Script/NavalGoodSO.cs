using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Inventory", menuName = "Mini Game/Goods", order = 2)]
public class NavalGoodSO : ScriptableObject
{
    public List<NavalItem> itemInventory;
}
