using UnityEngine;

public class BlinkButtonReaction : Reaction
{
    protected override void React()
    {
        ShopButtonBlinker.OnStartBlink?.Invoke();
    }
}