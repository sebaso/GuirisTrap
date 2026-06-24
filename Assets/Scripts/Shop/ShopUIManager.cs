using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopUIManager : MonoBehaviour
{
    private Dictionary<string, int> inventory = new Dictionary<string, int>();
    
    public void CountItems()
        {
            inventory.Clear();

            PlaceableItemData[] allItems = SaveManager.Instance.AllItems;

            if (allItems == null) return;

            for (int i = 0; i < allItems.Length; i++)
            {
                string itemName = allItems[i].name;

                if (inventory.ContainsKey(itemName))
                {
                    inventory[itemName]++;
                }
                else
                {
                    inventory.Add(itemName, 1);
                }
            }
        }
    public int GetCountItem(String itemName)
    {
        if(inventory == null) return -1;

        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }
        return -1;
    }
}
