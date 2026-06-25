using UnityEngine;

public class FornitureClickButton : MonoBehaviour
{
    public void OnFornitureClickButton()
    {
        TutorialEvents.OnEnteredForniture?.Invoke();
    }
}
