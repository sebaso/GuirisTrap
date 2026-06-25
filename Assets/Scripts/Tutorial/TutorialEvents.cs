using System;

public static class TutorialEvents
{
    public static Action<PlaceableItemData> OnItemBought;
    public static Action OnEnteredShop;
    public static Action OnEnteredForniture;
    public static Action OnExitedShop;
    public static Action OnClosedInventory;
}