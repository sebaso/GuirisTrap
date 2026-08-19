using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "FoodTagCatalogue", menuName = "Clients/Food Tag Catalogue")]
public class FoodTagCatalogue : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public RecipeData recipe;
        public FoodTag tags;
    }

    [Tooltip("Una fila por receta. Las que falten cuentan como None.")]
    public Entry[] entries;

    private Dictionary<RecipeData, FoodTag> _lookup;

    /// <summary>Tags de esta receta (None si no está en la tabla).</summary>
    public FoodTag GetTags(RecipeData recipe)
    {
        if (recipe == null) return FoodTag.None;

        if (_lookup == null)
        {
            _lookup = new Dictionary<RecipeData, FoodTag>();
            if (entries != null)
            {
                foreach (Entry e in entries)
                    if (e != null && e.recipe != null)
                        _lookup[e.recipe] = e.tags;
            }
        }

        return _lookup.TryGetValue(recipe, out FoodTag t) ? t : FoodTag.None;
    }

    /// <summary>Fuerza a reconstruir la tabla (tras editarla en runtime).</summary>
    public void InvalidateCache() => _lookup = null;
}
