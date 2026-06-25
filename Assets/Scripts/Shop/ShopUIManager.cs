using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopUIManager : MonoBehaviour
{
    public int GetCountItem(string itemName)
    {
        if (OwnedItemsManager.Instance == null) return 0;
        return OwnedItemsManager.Instance.GetCount(itemName);
    }
}
