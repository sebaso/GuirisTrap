using UnityEngine;

public class ShopOpenButton : MonoBehaviour
{
    public void OnOpenShopButton()
    {
        ShopEvents.OnEnteredShop?.Invoke();
    }
}
