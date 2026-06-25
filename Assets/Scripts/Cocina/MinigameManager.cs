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
        if (recipe == null)
        {
            Debug.LogError("[MinigameManager] La receta es null. No se puede lanzar el minijuego.");
            return;
        }

        switch (recipe.type)
        {
            case MinigameType.Nevera:
                if (!Check(fridgeGame, recipe.type)) return;
                fridgeGame.StartMinigame(recipe, player);
                break;

            case MinigameType.Congelador:
                if (!Check(freezerGame, recipe.type)) return;
                freezerGame.StartMinigame(recipe, player);
                break;

            case MinigameType.Despensa:
                if (!Check(pantryGame, recipe.type)) return;
                pantryGame.StartMinigame(recipe, player);
                break;

            case MinigameType.Especias:
                if (!Check(especiasGame, recipe.type)) return;
                especiasGame.StartMinigame(recipe, player);
                break;

            default:
                Debug.LogError($"[MinigameManager] Tipo de minijuego desconocido: {recipe.type}");
                break;
        }
    }


    private bool Check(Object game, MinigameType type)
    {
        if (game != null) return true;

        Debug.LogError($"[MinigameManager] El minijuego de '{type}' NO está asignado en el " +
                       $"Inspector del MinigameManager. Arrastra el script correspondiente al campo.");
        InputManager.Instance?.ExitMinigame();
        return false;
    }
}