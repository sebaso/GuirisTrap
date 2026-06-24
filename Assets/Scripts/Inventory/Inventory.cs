using UnityEngine;

public class Inventory : MonoBehaviour
{
    public System.Action OnInventoryChanged;
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
            // Destruir el GameObject del que ya existe el singleton
            if (gameObject != _instance.gameObject)
                Destroy(gameObject);
            else
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
                    OnInventoryChanged?.Invoke();
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
                    OnInventoryChanged?.Invoke();
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
        OnInventoryChanged?.Invoke();
        return true;
    }
}
