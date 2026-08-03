using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] 
    private Inventory _inventory;

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
        if(_instance == null)
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

        if (itemData == null || itemData.prefab == null)
        {
            Debug.LogWarning("No hay prefab");
            return;
        }
        HUDMessage.Instance?.ShowWarning("No hay espacio para colocar el objeto aquí.");
    }
}
