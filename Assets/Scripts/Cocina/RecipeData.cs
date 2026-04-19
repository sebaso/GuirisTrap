using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string dishName;
    public MinigameType type; 
    [Range(1, 4)] public int difficulty = 1;
    public float timeLimit = 5f;
    public GameObject foodPrefab;

    [Header("Solo para el minijuego de Especias")]
    public int balas = 5;
    public GameObject[] posiblesLayouts; 
}

public enum MinigameType { Nevera, Congelador, Despensa, Especias }