using UnityEngine;

public class BackInventoryClickButton : MonoBehaviour
{
    public void OnBackInventoryClickButton()
    {
        TutorialEvents.OnClosedInventory?.Invoke();
    }
}
