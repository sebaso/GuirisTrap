using UnityEngine;

public class Inventory : MonoBehaviour
{
    public System.Action OnInventoryChanged;
    [SerializeField] 
    private int _width = 7;
    [SerializeField] 
    private int _height = 4;
    private InventorySlot[,] _inventory;
    public int Width => _width;
    public int Height => _height;
    private static Inventory _instance;
    public static Inventory Instance => _instance; 
    
    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
                    Debug.Log($"Inventory creado en escena: {gameObject.scene.name}");
        }
        else
        {
                    Debug.Log($"Inventory duplicado destruido en escena: {gameObject.scene.name}");
            Destroy(this);
        }
    }
    public void Init()
    {
        _inventory = new InventorySlot[_width,_height];
    }
    public bool AddItem(PlaceableItemData item)
    {
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
        if(x >= 0 && x < _width && y >= 0 && y < _height)
            return _inventory[x,y];
        return null;
    }
    public bool RemoveItem(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
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
