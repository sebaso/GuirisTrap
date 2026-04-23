using UnityEngine;
[System.Serializable]
public class InventorySlot
{
    public PlaceableItemData item;
    public int quantity;
    public int maxStack;
    public bool CanStack(PlaceableItemData newItem)
    {
        if( item!= null && newItem != null && item == newItem && quantity < maxStack)
            return true;
        return false;
    }
    public void AddItem()
    {
        quantity++;
    }
}
