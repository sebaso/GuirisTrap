using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private Inventory _inventory;
    [SerializeField] 
    private GameGridManager _gridManager;
    private static GameManager _instance;
    public static GameManager Instance => _instance; 
    
    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
        _inventory.Init();
    }
    public void Buy(PlaceableItemData itemData)
    {
        if (itemData == null)
            return;

        // Check and deduct cost before buying
        if (CashManager.Instance != null && !CashManager.Instance.TrySpend(itemData.cost))
        {
            Debug.Log($"[GameManager] No tienes suficiente dinero para comprar: {itemData.prefab.name} (coste: {itemData.cost}€)");
            return;
        }

        bool added = _inventory.AddItem(itemData);
        if (added)
        {
            Debug.Log($"[GameManager] Has comprado: {itemData.prefab.name} por {itemData.cost}€");
        }
        else
        {
            // Refund if inventory is full
            CashManager.Instance?.Earn(itemData.cost);
            Debug.Log("[GameManager] No has podido comprar el item, inventario lleno. Dinero devuelto.");
        }
    }
    public void Place(int posX, int posY)
    {
        var slot = _inventory.GetSlot(posX, posY);
        PlaceableItemData itemData = slot.item;
        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (itemData == null || itemData.prefab == null || _gridManager == null)
        {
            Debug.LogWarning("No hay prefab o grid asignado en BuildManager");
            return;
        }
        for(int y = 0; y < _gridManager.GetGridData.height; y++)
        {
            for(int x = 0; x < _gridManager.GetGridData.width; x++)
            {
                if(_gridManager.GetGridData.GetType(x,y) == CellType.Empty)
                {
                    Vector3 initialPosition = new Vector3(x, 0.3f, y);
                    Vector3 finalPosition = initialPosition + itemData.placementOffset;
                    GameObject obj = Instantiate(itemData.prefab, finalPosition, Quaternion.identity, folder);

                    PlaceableObject placeable = obj.GetComponent<PlaceableObject>();

                    _gridManager.GetGridData.SetType(x, y, CellType.Occupied);
                    _gridManager.GetGridData.SetItem(x, y, itemData);

                    placeable.SetGridManager(_gridManager);
                    placeable.InstancePlaceableObjectCreated(x,y);
                    placeable.Init(itemData);

                    _gridManager.SetPlaceableAt(x,y, placeable);
                    _inventory.RemoveItem(posX, posY);
                    if(itemData.category == PlaceableCategory.Chair || itemData.category == PlaceableCategory.Table)
                        _gridManager.ValidateAllChairs();
                    return;
                }
            }
        }
    }
}