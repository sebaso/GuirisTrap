using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Guiri/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string dishName;
    public MinigameType type; 
    [Range(1, 4)]  public int difficulty = 1;
    public float timeLimit = 5f;
    public GameObject ingredientPrefab; 
}

public enum MinigameType { Nevera, Congelador, Despensa }