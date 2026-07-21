using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single source of truth for every recipe available in the game.
/// Populate the <see cref="all"/> array in the inspector with all RecipeData
/// assets (the 14 recipes under Assets/Prefabs/Receta/).
///
/// Used by <see cref="ClientGroup.GenerateOrder"/> to pick dishes for a group,
/// and by <see cref="PlayerController.CreateAndHoldFood"/> to resolve a recipe
/// from its foodPrefab when the source recipe wasn't threaded through (e.g. the
/// Espeto minigame, which cooks a fixed prefab).
/// </summary>
public class RecipeCatalogue : MonoBehaviour
{
    public static RecipeCatalogue Instance { get; private set; }

    [Tooltip("All recipes available for clients to order. Assign in the inspector.")]
    public RecipeData[] all;

    // foodPrefab → recipe lookup, built once at Awake.
    private Dictionary<GameObject, RecipeData> _byPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _byPrefab = new Dictionary<GameObject, RecipeData>();
        if (all == null) return;
        foreach (var r in all)
        {
            if (r != null && r.foodPrefab != null && !_byPrefab.ContainsKey(r.foodPrefab))
                _byPrefab[r.foodPrefab] = r;
        }
    }

    /// <summary>A uniformly random recipe from the catalogue.</summary>
    public RecipeData RandomRecipe()
    {
        if (all == null || all.Length == 0) return null;
        return all[Random.Range(0, all.Length)];
    }

    /// <summary>Resolves the RecipeData whose foodPrefab matches the given prefab.
    /// Returns null if none match (e.g. an unregistered/legacy prefab).</summary>
    public RecipeData FindByPrefab(GameObject prefab)
    {
        if (prefab == null || _byPrefab == null) return null;
        return _byPrefab.TryGetValue(prefab, out var r) ? r : null;
    }
}
