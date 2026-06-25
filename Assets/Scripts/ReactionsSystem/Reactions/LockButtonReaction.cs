using System.Collections.Generic;
using UnityEngine;

public class LockButtonReaction : Reaction
{
    [SerializeField] 
    private List<ButtonLocker> _buttonsLocker;
    [SerializeField] 
    private bool _lock = true;

    protected override void React()
    {
        if(_buttonsLocker == null) return;
        for(int i = 0; i < _buttonsLocker.Count; i++)
        {
            if(_buttonsLocker[i] != null)
                _buttonsLocker[i].SetLocked(_lock);
        }
    }
}
