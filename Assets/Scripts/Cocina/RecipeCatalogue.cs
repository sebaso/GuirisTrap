using System.Collections.Generic;
using UnityEngine;


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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    /// <summary>A uniformly random recipe from the catalogue.</summary>
    [System.Serializable]
    public class RecipeWeight
    {
        public RecipeData recipe;
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>Configura la frecuencia de pedidos de cada tipo de receta.</summary>
    [Header("Frecuencia de pedidos")]
    [Min(0f)] public float pesoSarten = 1f;          // MinigameType.Nevera
    [Min(0f)] public float pesoHorno = 1f;           // MinigameType.Congelador
    [Min(0f)] public float pesoTablaDeCortar = 1f;   // MinigameType.Despensa
    [Min(0f)] public float pesoMortero = 0.2f;       // MinigameType.Especias

    public RecipeWeight[] excepcionesPorReceta;

    /// <summary>Cuánto de probable es que pidan esta receta. Manda la excepción
    /// por receta si la tiene; si no, el peso de su tipo.</summary>
    public float WeightOf(RecipeData recipe)
    {
        if (recipe == null) return 0f;

        if (excepcionesPorReceta != null)
        {
            foreach (RecipeWeight e in excepcionesPorReceta)
                if (e != null && e.recipe == recipe) return Mathf.Max(0f, e.weight);
        }

        return recipe.type switch
        {
            MinigameType.Nevera     => Mathf.Max(0f, pesoSarten),
            MinigameType.Congelador => Mathf.Max(0f, pesoHorno),
            MinigameType.Despensa   => Mathf.Max(0f, pesoTablaDeCortar),
            MinigameType.Especias   => Mathf.Max(0f, pesoMortero),
            _                       => 1f,
        };
    }

    /// <summary>Elige una receta al azar respetando los pesos.</summary>
    public RecipeData RandomRecipe()
    {
        if (all == null || all.Length == 0) return null;

        float total = 0f;
        foreach (RecipeData r in all) total += WeightOf(r);

        // Todos los pesos a 0 (o mal configurados): se reparte por igual en vez
        // de dejar a los clientes sin poder pedir nada.
        if (total <= 0f) return all[Random.Range(0, all.Length)];

        float roll = Random.value * total;
        foreach (RecipeData r in all)
        {
            roll -= WeightOf(r);
            if (roll <= 0f) return r;
        }

        return all[all.Length - 1]; // por redondeos de coma flotante
    }

    public RecipeData FindByPrefab(GameObject prefab)
    {
        if (prefab == null || _byPrefab == null) return null;
        return _byPrefab.TryGetValue(prefab, out var r) ? r : null;
    }
}