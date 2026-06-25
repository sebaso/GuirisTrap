using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private GridController _gridController;
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    // Always resolve to the live singleton so every system (Buy, Place, UI)
    // shares one inventory. The serialized field is only a last-resort fallback;
    // a panel-baked Inventory that starts inactive must never become the source
    // of truth, because its Awake (and thus singleton registration) won't run
    // until the panel is first opened.
    private Inventory Inv
    {
        get
        {
            if (Inventory.Instance != null) return Inventory.Instance;
            if (_inventory != null) return _inventory;
            return Inventory.EnsureExists();
        }
    }

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
        // Establish the inventory singleton at scene start, before the player can
        // buy anything, so purchases never spawn a second detached instance just
        // because the inventory panel hasn't been opened yet.
        Inventory.EnsureExists().Init();
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

        Inventory inv = Inv;
        if (inv == null)
        {
            Debug.LogError("[GameManager] No Inventory encontrado.");
            MoneyManager.Instance?.AddMoney(itemData.cost);
            return;
        }

        bool added = inv.AddItem(itemData);
        if (added)
        {
            Debug.Log($"[GameManager] Has comprado: {itemData.prefab.name} por {itemData.cost}€");
            TutorialEvents.OnItemBought?.Invoke(itemData);
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
        Inventory inv = Inv;
        if (inv == null)
        {
            Debug.LogWarning("[GameManager] No Inventory encontrado al colocar.");
            return;
        }

        InventorySlot slot = inv.GetSlot(posX, posY);
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
                inv.RemoveItem(posX, posY);

                if (itemData.category == PlaceableCategory.Chair || itemData.category == PlaceableCategory.Table)
                    activeManager.ValidateAllChairs();

                return;
            }
        }

        Debug.Log("No hay espacio en el grid activo");
        HUDMessage.Instance?.ShowWarning("No hay espacio para colocar el objeto aquí.");
    }
}
