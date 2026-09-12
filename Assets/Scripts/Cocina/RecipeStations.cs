using UnityEngine;



public static class RecipeStations
{
    /// <summary>Nombre corto, para listas y comandas. Ej: "Sartén".</summary>
    public static string ShortName(MinigameType type) => type switch
    {
        MinigameType.Nevera     => "Sartén",
        MinigameType.Congelador => "Horno",
        MinigameType.Despensa   => "Tabla",
        MinigameType.Especias   => "Mortero",
        _                       => "?",
    };

    /// <summary>Nombre con artículo, para frases.".</summary>
    public static string LongName(MinigameType type) => type switch
    {
        MinigameType.Nevera     => "la SARTÉN",
        MinigameType.Congelador => "el HORNO",
        MinigameType.Despensa   => "la TABLA DE CORTAR",
        MinigameType.Especias   => "el MORTERO",
        _                       => "???",
    };

    /// <summary>Color para distinguir cada estación de un vistazo.</summary>
    public static string ColorHex(MinigameType type) => type switch
    {
        MinigameType.Nevera     => "#FF9B54", // sartén
        MinigameType.Congelador => "#6EC5FF", // horno
        MinigameType.Despensa   => "#8BD17C", // tabla
        MinigameType.Especias   => "#D9A0FF", // mortero
        _                       => "#FFFFFF",
    };

    /// <summary>"Paella <color=…>· Sartén</color>", listo para TextMeshPro.</summary>
    public static string DishWithStation(RecipeData recipe, bool colored = true)
    {
        if (recipe == null) return "?";

        string station = ShortName(recipe.type);
        if (!colored) return $"{recipe.dishName} · {station}";

        return $"{recipe.dishName} <color={ColorHex(recipe.type)}>· {station}</color>";
    }
}
