using UnityEngine;

public class FoodShopClickButton : MonoBehaviour
{
    public void OnFoodShopeClickButton()
    {
        TutorialEvents.OnEnteredFoodShop?.Invoke();
    }
}
