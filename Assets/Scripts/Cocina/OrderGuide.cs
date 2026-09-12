using System.Collections.Generic;
using UnityEngine;



public static class OrderGuide
{
    private const float CacheSeconds = 0.3f;

    private static readonly HashSet<RecipeData> _wanted = new();
    private static float _nextRefresh = -1f;

    /// <summary>¿Hay algún cliente esperando este plato ahora mismo?</summary>
    public static bool IsWanted(RecipeData recipe)
    {
        if (recipe == null) return false;
        Refresh();
        return _wanted.Contains(recipe);
    }

    /// <summary>Todas las recetas pendientes de servir.</summary>
    public static IReadOnlyCollection<RecipeData> Wanted
    {
        get { Refresh(); return _wanted; }
    }

    /// <summary>Fuerza a recalcular en la próxima consulta.</summary>
    public static void Invalidate() => _nextRefresh = -1f;

    private static void Refresh()
    {
        if (Time.time < _nextRefresh) return;
        _nextRefresh = Time.time + CacheSeconds;

        _wanted.Clear();

        foreach (Table t in Object.FindObjectsByType<Table>(FindObjectsSortMode.None))
        {
            ClientGroup g = t != null ? t.OccupyingGroup : null;
            if (g == null || g.Order == null || g.AllFed) continue;

            // Solo cuentan los grupos que están esperando de verdad: los que ya
            // están comiendo o largándose no necesitan que les cocines nada.
            bool esperando = false;
            foreach (Client m in g.Members)
            {
                if (m != null && m.CurrentState == Client.State.WaitingForFood) { esperando = true; break; }
            }
            if (!esperando) continue;

            foreach (RecipeData r in g.Order)
                if (r != null) _wanted.Add(r);
        }
    }
}