using UnityEngine;
[System.Serializable]
public class InventorySlot
{
    public PlaceableItemData item;
    public int tierIndex;
    public int quantity;
    public int maxStack;

    public bool CanStack(PlaceableItemData newItem, int newTierIndex)
    {
        if (item != null && newItem != null && item == newItem && tierIndex == newTierIndex && quantity < maxStack)
            return true;
        return false;
    }
    public void AddItem()
    {
        quantity++;
    }
}