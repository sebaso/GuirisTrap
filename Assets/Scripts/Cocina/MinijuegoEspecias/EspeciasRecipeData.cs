using UnityEngine;

[CreateAssetMenu(fileName = "NewEspeciasRecipe", menuName = "Recipe Data/Especias Recipe")]
public class EspeciasRecipeData : RecipeData
{
    [Header("Configuración Minijuego Especias")]
    [Tooltip("Número de balas disponibles para este plato.")]
    public int balas = 5;
}