using UnityEngine;
using TMPro;

public class FoodStorage : MonoBehaviour, IMinigameControllable
{
    [Header("Configuración")]
    public RecipeData[] recipes;
    public GameObject   selectionPopup;
    public TMP_Text     recipeNameText;

    private int              selectedIndex = 0;
    private bool             isPlayerClose = false;
    private bool             isSelecting   = false;
    private PlayerController playerRef;
    private float            _navCooldown  = 0f;

    private void Start()
    {
        if (selectionPopup) selectionPopup.SetActive(false);
    }

    private void Update()
    {
        if (_navCooldown > 0f) _navCooldown -= Time.deltaTime;

        // Detectar entrada del jugador para abrir el menú
        if (isPlayerClose && !isSelecting)
        {
            // La apertura la hace OnInteractDown via PlayerController
            // pero como PlayerController no sabe de FoodStorage,
            // usamos el trigger para detectar proximidad y
            // el InteractDown del player redirige aquí via CookingStation pattern
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerClose = true;
        playerRef     = other.GetComponent<PlayerController>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerClose = false;
        if (isSelecting) CloseSelection();
    }

    // Llamado desde PlayerController cuando está cerca y pulsa E
    public void TryOpen()
    {
        if (isPlayerClose && !isSelecting) OpenSelection();
    }

    void OpenSelection()
    {
        isSelecting = true;
        InputManager.Instance.EnterMinigame(this);
        selectionPopup.SetActive(true);
        selectedIndex = 0;
        UpdatePopupUI();
        AudioManager.Instance?.PlaySFX("popup_open");
    }

    void CloseSelection()
    {
        isSelecting = false;
        InputManager.Instance.ExitMinigame();
        selectionPopup.SetActive(false);
        AudioManager.Instance?.PlaySFX("popup_close");
    }

    void ChangeSelection(int direction)
    {
        if (_navCooldown > 0f) return;
        selectedIndex += direction;
        if (selectedIndex >= recipes.Length) selectedIndex = 0;
        if (selectedIndex < 0) selectedIndex = recipes.Length - 1;
        _navCooldown = 0.25f;
        UpdatePopupUI();
    }

    void UpdatePopupUI()
    {
        RecipeData current = recipes[selectedIndex];

        // Stock en su propia línea, justo debajo del nombre del plato.
        // Si el sistema de ingredientes no está montado, esa línea no aparece.
        string stockLine = "";
        int stock = IngredientStockManager.GetStock(current);
        if (stock >= 0)   // -1 = esta receta no lleva control de stock
        {
            stockLine = stock > 0
                ? $"x{stock}\n"
                : $"<color=#FF6B6B>SIN STOCK \u00B7 {IngredientStockManager.EmergencyPrice(current)}\u20AC urgencia</color>\n";
        }

        // Si algún cliente está esperando justo este plato, se resalta con el
        // color de su estación (el mismo de la comanda y de las flechas), para
        // que no haya que ir comparando la lista con la comanda a ojo.
        string nombre = current.dishName;
        if (OrderGuide.IsWanted(current))
            nombre = $"<color={RecipeStations.ColorHex(current.type)}>{current.dishName}  \u25C4 LO PIDEN</color>";

        recipeNameText.text = $"{nombre}\n{stockLine}(Ir a: {GetDestinationName(current.type)})";
    }
    
    string GetDestinationName(MinigameType type) => RecipeStations.LongName(type);

    void ConfirmSelection()
    {
        RecipeData chosenRecipe = recipes[selectedIndex];

        // El electrodoméstico comprueba que tienes con qué cocinarlo. Si no
        // queda stock hace una compra de urgencia (más cara); si tampoco hay
        // dinero, no te llevas el ingrediente.
        if (!IngredientStockManager.TryTakeIngredient(chosenRecipe))
        {
            UpdatePopupUI();
            return;
        }

        string destino = GetDestinationName(chosenRecipe.type);
        Debug.Log($"<color=cyan>RECETA ELEGIDA: {chosenRecipe.dishName}. VE A: {destino}</color>");
        playerRef.SetCurrentIngredients(chosenRecipe);
        CloseSelection();
    }

    //  IMinigameControllable 

    public void OnInteract() => ConfirmSelection();

    public void OnNavigate(Vector2 direction)
    {
        if (direction.x > 0.5f)       ChangeSelection(1);
        else if (direction.x < -0.5f) ChangeSelection(-1);
    }

    public void OnCancel()  => CloseSelection();
    public void OnSubmit()  => ConfirmSelection();
}