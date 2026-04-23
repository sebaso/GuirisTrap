using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance;

    [Header("Minigame Scripts")]
    public NeveraMinigame     fridgeGame;
    public CongeladorMinigame freezerGame;
    public DespensaMinigame   pantryGame;
    public EspeciasMinigame   especiasGame;

    void Awake() { Instance = this; }

    public void LaunchMinigame(RecipeData recipe, PlayerController player)
    {
        switch (recipe.type)
        {
            case MinigameType.Nevera:
                fridgeGame.StartMinigame(recipe, player);
                break;
            case MinigameType.Congelador:
                freezerGame.StartMinigame(recipe, player);
                break;
            case MinigameType.Despensa:
                pantryGame.StartMinigame(recipe, player);
                break;
            case MinigameType.Especias:
                especiasGame.StartMinigame(recipe, player);
                break;
        }
    }
}