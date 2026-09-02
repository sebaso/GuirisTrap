using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "IngredientCatalogue", menuName = "Cocina/Ingredient Catalogue")]
public class IngredientCatalogue : ScriptableObject
{
    [System.Serializable]
    public class Pack
    {
        [Tooltip("Nombre que se ve en la tienda. Ej: PACK RECETAS NEVERA")]
        public string displayName = "PACK RECETAS";

        [Tooltip("Electrodoméstico al que pertenece. .")]
        public MinigameType type;

        [Tooltip("Unidades que añade a CADA receta de ese tipo.")]
        public int unitsPerRecipe = 10;

        [Tooltip("Precio del pack entero.")]
        public int price = 120;

        public bool firstOneFree = true;

        public int emergencyUnitPrice = 25;

        public Sprite icon;
    }

    [Header("Recetas del juego")]

    public RecipeData[] allRecipes;

    [Header("Partida nueva")]
    public int startingStockPerRecipe = 10;

    [Header("Packs de la tienda")]
    public List<Pack> packs = new();

    private HashSet<RecipeData> _managed;

    void OnEnable()   => _managed = null;
    void OnValidate() => _managed = null;

    /// <summary>¿Esta receta lleva control de stock? (está en All Recipes)</summary>
    public bool Contains(RecipeData recipe)
    {
        if (recipe == null) return false;

        if (_managed == null)
        {
            _managed = new HashSet<RecipeData>();
            if (allRecipes != null)
                foreach (RecipeData r in allRecipes)
                    if (r != null) _managed.Add(r);
        }
        return _managed.Contains(recipe);
    }

    /// <summary>Recetas de un electrodoméstico concreto.</summary>
    public List<RecipeData> RecipesOfType(MinigameType type)
    {
        var list = new List<RecipeData>();
        if (allRecipes == null) return list;

        foreach (RecipeData r in allRecipes)
            if (r != null && r.type == type)
                list.Add(r);
        return list;
    }

    /// <summary>Pack al que pertenece una receta (null si no hay ninguno).</summary>
    public Pack PackForRecipe(RecipeData recipe)
    {
        if (recipe == null) return null;
        foreach (Pack p in packs)
            if (p != null && p.type == recipe.type)
                return p;
        return null;
    }

    /// <summary>Lo que costaría una unidad comprando el pack. Solo informativo,
    /// para poder comparar con el precio de emergencia desde el editor.</summary>
    public float UnitPriceInPack(Pack pack)
    {
        if (pack == null || pack.unitsPerRecipe <= 0) return 0f;
        int recipeCount = RecipesOfType(pack.type).Count;
        if (recipeCount == 0) return 0f;
        return pack.price / (float)(pack.unitsPerRecipe * recipeCount);
    }
}
