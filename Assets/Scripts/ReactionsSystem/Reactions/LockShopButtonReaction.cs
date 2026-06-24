using UnityEngine;

public class LockShopButtonReaction : Reaction
{
    [SerializeField] private bool _lock = true;

    protected override void React()
    {
        ShopEvents.OnShopButtonLocked?.Invoke(_lock);
    }
}
