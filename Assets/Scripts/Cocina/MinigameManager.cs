using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance;

    [Header("Minigame Scripts")]
    public NeveraMinigame fridgeGame;     // NEVERA A SARTEN
    public CongeladorMinigame freezerGame; // CONGELADOR A HORNO
    public DespensaMinigame pantryGame; // DESPENSA A MESA

    void Awake() { Instance = this; }

    public void LaunchMinigame(RecipeData recipe, PlayerController player)
    {
        switch (recipe.type)
        {
            case MinigameType.Nevera:
                fridgeGame.StartMinigame(recipe, player);
                break;
                
            case MinigameType.Congelador:
                // AQUI LLAMAMOS AL NUEVO
                freezerGame.StartMinigame(recipe, player);
                break;
                
            case MinigameType.Despensa:
                pantryGame.StartMinigame(recipe, player);
                break;
        }
    }
}