using UnityEngine;
using TMPro; 

public class FoodStorage : MonoBehaviour
{
    [Header("Configuración")]
    public RecipeData[] recipes; 
    public GameObject selectionPopup; 
    public TMP_Text recipeNameText; 
    
    private int selectedIndex = 0;
    private bool isPlayerClose = false;
    private bool isSelecting = false;
    private PlayerController playerRef;

    private void Start()
    {
        if(selectionPopup) selectionPopup.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerClose && Input.GetKeyDown(KeyCode.E) && !isSelecting)
        {
            OpenSelection();
        }
        else if (isSelecting)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeSelection(1);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ChangeSelection(-1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmSelection();
            if (Input.GetKeyDown(KeyCode.Escape)) CloseSelection();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("Detección: El Jugador ha entrado.");
        isPlayerClose = true;
        playerRef = other.GetComponent<PlayerController>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerClose = false;
        if(isSelecting) CloseSelection();
    }

    void OpenSelection()
    {
        isSelecting = true;
        playerRef.enabled = false; // BLOQUEAMOS MOVIMIENTO
        selectionPopup.SetActive(true);
        selectedIndex = 0; 
        UpdatePopupUI();
    }

    void CloseSelection()
    {
        isSelecting = false;
        if(playerRef) playerRef.enabled = true; // DEBLOQUEAMOS MOVIMIENTO
        selectionPopup.SetActive(false);
    }

    void ChangeSelection(int direction)
    {
        selectedIndex += direction;
        if (selectedIndex >= recipes.Length) selectedIndex = 0;
        if (selectedIndex < 0) selectedIndex = recipes.Length - 1;
        UpdatePopupUI();
    }

    void UpdatePopupUI()
    {
        RecipeData current = recipes[selectedIndex];
        // MOSTRAR NOMBRE DE LA RECETA Y SU DESTINO
        recipeNameText.text = $"{current.dishName}\n(Ir a: {current.type})";
    }

    void ConfirmSelection()
    {
        RecipeData chosenRecipe = recipes[selectedIndex];
        
        string destino = "";
        switch(chosenRecipe.type) {
            case MinigameType.Nevera: destino = "la SARTÉN"; break;
            case MinigameType.Congelador: destino = "el HORNO"; break;
            case MinigameType.Despensa: destino = "la TABLA DE CORTAR"; break;
        }

        Debug.Log($"<color=cyan>RECETA ELEGIDA: {chosenRecipe.dishName}. VE A: {destino}</color>");
        
        playerRef.SetCurrentIngredients(chosenRecipe); 
        playerRef.ResetInput();
        CloseSelection();
    }
}