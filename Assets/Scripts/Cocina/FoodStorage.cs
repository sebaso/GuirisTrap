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
    }

    void CloseSelection()
    {
        isSelecting = false;
        InputManager.Instance.ExitMinigame();
        selectionPopup.SetActive(false);
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
        recipeNameText.text = $"{current.dishName}\n(Ir a: {current.type})";
    }

    void ConfirmSelection()
    {
        RecipeData chosenRecipe = recipes[selectedIndex];
        string destino = chosenRecipe.type switch
        {
            MinigameType.Nevera     => "la SARTÉN",
            MinigameType.Congelador => "el HORNO",
            MinigameType.Despensa   => "la TABLA DE CORTAR",
            MinigameType.Especias   => "el MORTERO",
            _                       => "???"
        };
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