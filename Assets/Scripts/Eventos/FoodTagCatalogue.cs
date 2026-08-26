using System.Collections.Generic;
using UnityEngine;

// Tabla receta -> tags. Vive en un asset aparte para no tocar RecipeData.
// Asignarlo en el campo Tag Catalogue del SpecialClientManager.

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

    // El caché se reconstruye solo si tocas la tabla en el inspector (incluso
    // con el juego corriendo) o al recargar el asset. Sin esto, afinar tags en
    // Play mode no tenía efecto hasta salir y volver a entrar.
    void OnEnable()   => _lookup = null;
    void OnValidate() => _lookup = null;

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

    public void InvalidateCache() => _lookup = null;
}
