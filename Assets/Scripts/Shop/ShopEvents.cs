using System;

public static class ShopEvents
{
    public static Action<PlaceableItemData> OnItemBought;
    public static Action OnEnteredShop;
    public static Action OnExitedShop;
    public static Action<bool> OnShopButtonLocked;
}