using UnityEngine;
using UnityEngine.UI;

public class BlinkButtonReaction : Reaction
{
    [SerializeField] 
    private ButtonBlinker _blinker;
    [SerializeField] 
    private bool _blink = true;

    protected override void React()
    {
        if (_blink) _blinker.StartBlink();
        else _blinker.StopBlink();
    }
}