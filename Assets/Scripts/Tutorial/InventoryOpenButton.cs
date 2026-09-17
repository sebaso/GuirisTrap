using UnityEngine;

public class InventoryOpenButton : MonoBehaviour
{
    public void OnOpenInventoryButton()
    {
        TutorialEvents.OnInventoryEntered?.Invoke();
    }
}
