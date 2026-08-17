using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] 
    private Inventory _inventory;
    [SerializeField] 
    private GridZone _zone;
    [SerializeField] 
    private CameraController _cameraController;

    private static GameManager _instance;
    public static GameManager Instance => _instance;

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
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
        Inventory.EnsureExists().Init();
    }

    public void Buy(PlaceableItemData itemData)
    {
        if (itemData == null)
            return;

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
            OwnedItemsManager.Instance?.AddItem(itemData.name);
            TutorialEvents.OnItemBought?.Invoke(itemData);
            HUDMessage.Instance?.ShowGood($"¡Comprado! {itemData.prefab.name} por {itemData.cost}€");
        }
        else
        {
            MoneyManager.Instance?.AddMoney(itemData.cost);
            Debug.Log("[GameManager] No has podido comprar el item, inventario lleno. Dinero devuelto.");
            HUDMessage.Instance?.ShowWarning("Inventario lleno. Dinero devuelto.");
        }
    }

    public void Place(int posX, int posY)
    {
        Inventory inv = Inv;
        if (inv == null) return;

        InventorySlot slot = inv.GetSlot(posX, posY);
        if (slot == null) return;

        PlaceableItemData itemData = slot.item;
        if (itemData == null || itemData.prefab == null || _zone == null
            || _zone.GridManager == null || _zone.Resolver == null || _zone.Registry == null
            || _cameraController == null)
            return;

        GridManager gridManager = _zone.GridManager;
        IGridWorldResolver resolver = _zone.Resolver;
        PlaceableInstanceRegistry registry = _zone.Registry;

        CameraView view = _cameraController.CurrentView;
        PlaceableSurface activeSurface = GridProjectionVisibility.SurfaceForView(view);

        if (!itemData.IsCompatibleWith(activeSurface))
        {
            HUDMessage.Instance?.ShowWarning("Este objeto no se puede colocar aquí.");
            return;
        }

        if (!gridManager.TryFindFreeCellInLayer(view, itemData, out Vector3Int cell))
        {
            HUDMessage.Instance?.ShowWarning("No hay espacio para colocar el objeto aquí.");
            return;
        }

        if (!resolver.TryGetWorldTransform(view, cell, out Vector3 basePos, out Quaternion baseRot))
        {
            Debug.LogWarning($"[GameManager] No se pudo resolver transform de mundo para {cell}.");
            return;
        }

        PlacementAxis axis = GridManager.AxisForView(view);
        if (!gridManager.PlaceItem(cell.x, cell.y, cell.z, itemData, axis, baseRot))
            return;

        Vector3 worldPos = basePos + baseRot * itemData.placementOffset;

        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (folder == null) folder = new GameObject("PlaceableItems").transform;

        GameObject obj = Instantiate(itemData.prefab, worldPos, baseRot, folder);
        PlaceableObject placeable = obj.GetComponent<PlaceableObject>();
        placeable.Init(itemData);
        placeable.InstancePlaceableObjectCreated(cell, view);

        registry.Register(cell, placeable);
        inv.RemoveItem(posX, posY);
    }
}