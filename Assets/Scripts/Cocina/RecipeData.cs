using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipe Data/Recipe")]
public class RecipeData : ScriptableObject
{
    public string dishName;
    public MinigameType type;
    [Range(1, 4)] public int difficulty = 1;
    public float timeLimit = 5f;
    public GameObject foodPrefab;

    [Tooltip("Optional 2D icon for order bubbles / ticket rail. Null = text-only.")]
    public Sprite icon;
}

public enum MinigameType { Nevera, Congelador, Despensa, Especias }
