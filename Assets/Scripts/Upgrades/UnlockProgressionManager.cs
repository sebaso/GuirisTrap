using System.Collections.Generic;
using UnityEngine;

public class UnlockProgressionManager : MonoBehaviour
{
    [SerializeField]
    private List<ShopItemUI> _items = new List<ShopItemUI>();

    [Header("Upgrades")]
    [SerializeField]
    private PlaceableItemData _table;

    void Start()
    {
        int days = SaveManager.Instance.CurrentDay;

        if (days >= 1) _items[0].SetEnable(true);
        if (days >= 2) _items[1].SetEnable(true);
        if (days >= 3) _items[2].SetEnable(true);
        if (days >= 5) _items[3].SetEnable(true);
        if (days >= 7) UpgradeManager.Instance.TryUpgrade(_table);
        if (days >= 13)
        {
            _items[4].SetEnable(true);
            _items[5].SetEnable(true);
        }
        if (days >= 15) _items[6].SetEnable(true);
        if (days >= 17) _items[7].SetEnable(true);
    }
}
