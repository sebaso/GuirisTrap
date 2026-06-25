using UnityEngine;

public class Inventory : MonoBehaviour
{
    public System.Action OnInventoryChanged;
    // ponytail: static event so UI (and any listener) catches changes regardless
    // of which Inventory instance is currently the singleton. Simpler than
    // having the UI re-bind when the singleton swaps.
    public static event System.Action OnAnyInventoryChanged;
    [SerializeField] 
    private int _width = 7;
    [SerializeField] 
    private int _height = 4;
    private InventorySlot[,] _inventory;
    private bool _isInitialized = false;
    public int Width => _width;
    public int Height => _height;
    private static Inventory _instance;
    public static Inventory Instance => _instance;

    // ponytail: lazy-create so callers (GameManager.Buy) work even if the
    // Inventory GameObject is buried in a closed UI panel that hasn't awoken yet.
    // If a panel-baked Inventory wakes up later, it destroys itself as a dup.
    public static Inventory EnsureExists()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("Inventory");
        go.AddComponent<Inventory>();
        return _instance;
    }
    
    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Init();
            Debug.Log($"Inventory creado en escena: {gameObject.scene.name}");
        }
        else
        {
            Debug.Log($"Inventory duplicado destruido en escena: {gameObject.scene.name}");
            // Only destroy the duplicate component, never its GameObject:
            // the GameObject may host the InventoryUI / slots we still need.
            Destroy(this);
        }
    }
    public void Init()
    {
        if (_isInitialized) return; // No reinicializar si ya tiene datos
        _inventory = new InventorySlot[_width,_height];
        _isInitialized = true;
    }
    
    /// <summary>Limpia todo el inventario (para empezar fresco).</summary>
    public void Clear()
    {
        _inventory = new InventorySlot[_width, _height];
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
        OnAnyInventoryChanged?.Invoke();
    }

    public bool AddItem(PlaceableItemData item)
    {
        if (_inventory == null) Init();
        if (_inventory == null) return false;
        //Primero intento stackear el objeto
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var slot = _inventory[x, y];

                if (slot != null && slot.CanStack(item))
                {
                    slot.AddItem();
                    NotifyChanged();
                    return true;
                }
            }
        }
        //Si no, intento crearlo
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_inventory[x, y] == null)
                {
                    _inventory[x, y] = new InventorySlot
                    {
                        item = item,
                        quantity = 1,
                        maxStack = item.maxStack
                    };
                    NotifyChanged();
                    return true;
                }
            }
        }
        //Si no puedo, el inventario está lleno
        return false;
    }
    public InventorySlot GetSlot(int x, int y)
    {
        if (_inventory == null) Init();
        if (_inventory == null || x < 0 || x >= _width || y < 0 || y >= _height)
            return null;
        return _inventory[x,y];
    }
    public bool RemoveItem(int x, int y)
    {
        if (_inventory == null) Init();
        if (_inventory == null || x < 0 || x >= _width || y < 0 || y >= _height)
            return false;

        var slot = _inventory[x, y];

        if (slot == null) return false;

        slot.quantity--;

        if (slot.quantity <= 0)
        {
            _inventory[x, y] = null;
        }
        NotifyChanged();
        return true;
    }
}
