using UnityEngine;

public class ShopOpenButton : MonoBehaviour
{
    public void OnOpenShopButton()
    {
        TutorialEvents.OnEnteredShop?.Invoke();
    }
}