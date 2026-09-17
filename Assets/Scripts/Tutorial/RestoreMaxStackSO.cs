using UnityEngine;

public class RestoreMaxStackSO : MonoBehaviour
{
    [SerializeField]
    private PlaceableItemData  _chair;
    [SerializeField]
    private PlaceableItemData  _table;

    public void RestoreMaxStack()
    {
        _chair.maxStack = 24;
        _table.maxStack = 24;
    }
}
