using UnityEngine;

public abstract class MinigameBase : MonoBehaviour
{
    protected RecipeData currentRecipe;
    protected PlayerController player;
    public System.Action<bool> OnMinigameFinished;

    public abstract void Setup(RecipeData recipe, PlayerController p);
    
}