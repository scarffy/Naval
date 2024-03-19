using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Inventory
{
    public class BaseInventory : MonoBehaviour
    {
        public NavalGoodSO baseInventory;

        [Space]
        public int inventoryLimit = 0;
        public List<NavalItem> items;

        public virtual void AddItem(NavalItem item)
        {
            if (items.Count < inventoryLimit)
                items.Add(item);
        }

        public virtual void RemoveItem(NavalItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
            }
        }

        public virtual void SetInventoryLimit(int value)
        {
            inventoryLimit = value;
        }
    }
}
