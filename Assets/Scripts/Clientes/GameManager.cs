using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] 
    private  GridController _gridController;
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
        if (Inventory.Instance != null) Inventory.Instance.Init();
    }
    public void Buy(PlaceableItemData itemData)
    {
        if (itemData == null)
            return;

        // Check and deduct cost before buying
        if (MoneyManager.Instance != null && !MoneyManager.Instance.TrySpend(itemData.cost))
        {
            Debug.Log($"[GameManager] No tienes suficiente dinero para comprar: {itemData.prefab.name} (coste: {itemData.cost}€)");
            HUDMessage.Instance?.ShowBad($"No tienes suficiente dinero: {itemData.cost}€");
            return;
        }

        if (Inventory.Instance == null)
        {
            Debug.LogWarning("[GameManager] No Inventory encontrado. Devolviendo dinero.");
            MoneyManager.Instance?.AddMoney(itemData.cost);
            HUDMessage.Instance?.ShowWarning("Inventario no disponible. Dinero devuelto.");
            return;
        }

        bool added = Inventory.Instance.AddItem(itemData);
        if (added)
        {
            Debug.Log($"[GameManager] Has comprado: {itemData.prefab.name} por {itemData.cost}€");
            ShopEvents.OnItemBought?.Invoke(itemData);
            HUDMessage.Instance?.ShowGood($"¡Comprado! {itemData.prefab.name} por {itemData.cost}€");
        }
        else
        {
            // Refund if inventory is full
            MoneyManager.Instance?.AddMoney(itemData.cost);
            Debug.Log("[GameManager] No has podido comprar el item, inventario lleno. Dinero devuelto.");
            HUDMessage.Instance?.ShowWarning("Inventario lleno. Dinero devuelto.");
        }
    }
    public void Place(int posX, int posY)
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("[GameManager] No Inventory encontrado al colocar.");
            return;
        }

        InventorySlot slot = Inventory.Instance.GetSlot(posX, posY);
        if (slot == null) return;

        PlaceableItemData itemData = slot.item;
        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (folder == null)
            folder = new GameObject("PlaceableItems").transform;

        if (itemData == null || itemData.prefab == null || _gridController == null)
        {
            Debug.LogWarning("No hay prefab o GridController asignado en GameManager");
            return;
        }

        GameGridManager activeManager = _gridController.ActiveGridManager;
        if (activeManager == null) return;

        if (!itemData.IsCompatibleWith(activeManager.Surface))
        {
            Debug.Log("El objeto no es compatible con la superficie activa");
            HUDMessage.Instance?.ShowWarning("No se puede colocar aquí, superficie incompatible.");
            return;
        }

        GridData gridData = activeManager.GetGridData;

        for (int y = 0; y < gridData.height; y++)
        {
            for (int x = 0; x < gridData.width; x++)
            {
                if (gridData.GetType(x, y) != CellType.Empty) continue;

                Vector3 localPos = new Vector3(x, 0f, y) + itemData.placementOffset;
                Vector3 worldPos = activeManager.transform.TransformPoint(localPos);

                GameObject obj = Instantiate(itemData.prefab, worldPos, activeManager.transform.rotation, folder);
                PlaceableObject placeable = obj.GetComponent<PlaceableObject>();

                gridData.SetType(x, y, CellType.Occupied);
                gridData.SetItem(x, y, itemData);

                placeable.SetGridManager(activeManager);
                placeable.InstancePlaceableObjectCreated(x, y);
                placeable.Init(itemData);

                activeManager.SetPlaceableAt(x, y, placeable);
                Inventory.Instance.RemoveItem(posX, posY);

                if (itemData.category == PlaceableCategory.Chair || itemData.category == PlaceableCategory.Table)
                    activeManager.ValidateAllChairs();

                return;
            }
        }

        Debug.Log("No hay espacio en el grid activo");
        HUDMessage.Instance?.ShowWarning("No hay espacio para colocar el objeto aquí.");
    }
}
