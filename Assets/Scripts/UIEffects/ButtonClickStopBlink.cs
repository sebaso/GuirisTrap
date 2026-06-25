using UnityEngine;

public class ButtonClickStopBlink : MonoBehaviour
{
    public void OnButtonClick()
    {
        GetComponent<ButtonBlinker>().StopBlink();
    }
}
