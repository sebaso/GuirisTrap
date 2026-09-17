using System;

public static class TutorialEvents
{
    public static Action<PlaceableItemData> OnItemBought;
    public static Action OnEnteredShop;
    public static Action OnEnteredForniture;
    public static Action OnExitedShop;
    public static Action OnEnteredFoodShop;
    public static Action OnInventoryEntered;
    public static Action OnPlayChairError;
    public static Action OnPlayStoolError;
    public static Action OnPlayBarError;
    public static Action OnPlayMinimoError;
}