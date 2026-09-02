using System.Collections.Generic;
using UnityEngine;



public class IngredientStockManager : MonoBehaviour
{
    public static IngredientStockManager Instance { get; private set; }

    [SerializeField] private IngredientCatalogue _catalogue;
    public IngredientCatalogue Catalogue => _catalogue;


    private const string InitMarker = "__ingredientes_iniciados";

    // Prefijo para que las claves de comida no se mezclen con las de muebles.
    private const string Prefix = "ING_";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start() => EnsureInitialStock();

    private static string Key(RecipeData r) => r != null ? Prefix + r.name : null;

    /// <summary>True si el sistema está montado y operativo.</summary>
    public static bool IsActive =>
        Instance != null && Instance._catalogue != null && OwnedItemsManager.Instance != null;


    private void EnsureInitialStock()
    {
        if (_catalogue == null)
        {
            Debug.LogWarning("[IngredientStockManager] Sin IngredientCatalogue asignado: el stock queda desactivado.");
            return;
        }
        if (OwnedItemsManager.Instance == null) return;
        if (OwnedItemsManager.Instance.GetCount(InitMarker) > 0) return; // ya iniciado

        foreach (RecipeData r in _catalogue.allRecipes)
        {
            if (r == null) continue;
            OwnedItemsManager.Instance.AddItem(Key(r), _catalogue.startingStockPerRecipe);
        }

        OwnedItemsManager.Instance.AddItem(InitMarker, 1);
        Debug.Log($"[IngredientStockManager] Partida nueva: {_catalogue.startingStockPerRecipe} unidades de cada receta.");
        OnStockChanged?.Invoke();
    }


    public static bool IsManaged(RecipeData recipe)
    {
        if (!IsActive || recipe == null) return false;
        return Instance._catalogue.Contains(recipe);
    }

    /// <summary>Unidades que quedan. Devuelve -1 si la receta no lleva control de
    /// stock, para que quien lo pinte sepa que no debe mostrar número.</summary>
    public static int GetStock(RecipeData recipe)
    {
        if (!IsActive || recipe == null) return -1;
        if (!IsManaged(recipe)) return -1;

        Instance.EnsureInitialStock();
        return OwnedItemsManager.Instance.GetCount(Key(recipe));
    }

    public static bool HasStock(RecipeData recipe) => GetStock(recipe) != 0;

    /// <summary>Lo que costaría sacar una unidad ahora mismo sin stock.</summary>
    public static int EmergencyPrice(RecipeData recipe)
    {
        if (Instance == null || Instance._catalogue == null) return 0;
        IngredientCatalogue.Pack pack = Instance._catalogue.PackForRecipe(recipe);
        return pack != null ? pack.emergencyUnitPrice : 0;
    }


    public static bool TryTakeIngredient(RecipeData recipe)
    {
        if (recipe == null) return false;
        if (!IsActive) return true;        // sin sistema montado: barra libre
        if (!IsManaged(recipe)) return true; // receta sin control de stock

        Instance.EnsureInitialStock();
        string key = Key(recipe);

        if (OwnedItemsManager.Instance.TrySpendItem(key, 1))
        {
            int left = OwnedItemsManager.Instance.GetCount(key);
            if (left == 0)
                HUDMessage.Instance?.ShowWarning($"¡Última unidad de {recipe.dishName}! Repón en la tienda.");
            OnStockChanged?.Invoke();
            return true;
        }

        // Sin stock: compra de emergencia.
        int price = EmergencyPrice(recipe);

        if (price <= 0)
        {
            HUDMessage.Instance?.ShowBad($"No te queda {recipe.dishName} y no hay proveedor de urgencia.");
            AudioManager.Instance?.PlaySFX("error");
            Debug.LogWarning($"[IngredientStockManager] {recipe.dishName} se ha agotado y no hay " +
                             $"ningún pack de tipo {recipe.type} en el catálogo, así que no se puede " +
                             "ni reponer ni comprar de urgencia. Añade ese pack o ese electrodoméstico " +
                             "quedará inservible al acabarse.");
            return false;
        }

        if (MoneyManager.Instance == null || !MoneyManager.Instance.TrySpend(price))
        {
            HUDMessage.Instance?.ShowBad(
                $"Sin {recipe.dishName} y sin dinero para la compra de urgencia ({price}€).");
            AudioManager.Instance?.PlaySFX("error");
            return false;
        }

        DayReport.Instance?.RegisterSpending(price);
        HUDMessage.Instance?.ShowWarning(
            $"¡Compra de urgencia! {recipe.dishName} por {price}€. Repón packs en la tienda.");
        AudioManager.Instance?.PlaySFX("compra_urgencia");
        Debug.Log($"[IngredientStockManager] Compra de urgencia: {recipe.dishName} por {price}€.");
        return true;
    }

    // ---- Tienda ----

    /// <summary>¿Este pack sale gratis ahora mismo? (primera compra de la partida)</summary>
    public static bool IsPackFree(IngredientCatalogue.Pack pack)
    {
        if (pack == null || !IsActive) return false;
        if (!pack.firstOneFree) return false;
        return OwnedItemsManager.Instance.GetCount(ClaimKey(pack)) == 0;
    }

    /// <summary>Lo que cuesta el pack ahora mismo: 0 si es el primero gratis.</summary>
    public static int PriceOf(IngredientCatalogue.Pack pack)
    {
        if (pack == null) return 0;
        return IsPackFree(pack) ? 0 : pack.price;
    }

    private static string ClaimKey(IngredientCatalogue.Pack pack) => "__pack_gratis_" + pack.type;

    /// <summary>Compra un pack: cobra y reparte unidades a todas las recetas de
    /// ese electrodoméstico. Devuelve false si no llega el dinero.</summary>
    public static bool TryBuyPack(IngredientCatalogue.Pack pack)
    {
        if (pack == null || !IsActive) return false;

        Instance.EnsureInitialStock();
        List<RecipeData> recipes = Instance._catalogue.RecipesOfType(pack.type);
        if (recipes.Count == 0)
        {
            Debug.LogWarning($"[IngredientStockManager] El pack '{pack.displayName}' no tiene recetas de tipo {pack.type} en el catálogo.");
            return false;
        }

        bool isFree = IsPackFree(pack);

        if (!isFree)
        {
            if (MoneyManager.Instance == null)
            {
                Debug.LogWarning("[IngredientStockManager] No hay MoneyManager: no se puede cobrar el pack.");
                return false;
            }
            if (!MoneyManager.Instance.TrySpend(pack.price))
            {
                HUDMessage.Instance?.ShowBad($"No te llega para {pack.displayName} ({pack.price}€).");
                return false;
            }
        }
        else
        {
            // Marcar el gratis como usado ANTES de repartir, para que un doble
            // click rápido no cuele dos packs gratis.
            OwnedItemsManager.Instance.AddItem(ClaimKey(pack), 1);
        }

        foreach (RecipeData r in recipes)
            OwnedItemsManager.Instance.AddItem(Key(r), pack.unitsPerRecipe);

        HUDMessage.Instance?.ShowGood(isFree
            ? $"{pack.displayName} GRATIS: +{pack.unitsPerRecipe} unidades de CADA una de sus {recipes.Count} recetas."
            : $"{pack.displayName}: +{pack.unitsPerRecipe} unidades de CADA una de sus {recipes.Count} recetas.");

        AudioManager.Instance?.PlaySFX("compra");
        OnStockChanged?.Invoke();
        return true;
    }

    public static System.Action OnStockChanged;

    public static string StockBreakdownOfType(MinigameType type, string outOfStockHex = "#FF6B6B")
    {
        if (Instance == null || Instance._catalogue == null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (RecipeData r in Instance._catalogue.RecipesOfType(type))
        {
            if (r == null) continue;
            int n = GetStock(r);
            if (n < 0) continue; // sin control de stock

            if (sb.Length > 0) sb.Append('\n');
            if (n <= 0) sb.Append($"<color={outOfStockHex}>{r.dishName}  0</color>");
            else        sb.Append($"{r.dishName}  {n}");
        }
        return sb.ToString();
    }

    /// <summary>Stock total de un electrodoméstico, para pintarlo en la tienda.</summary>
    public static int StockOfType(MinigameType type)
    {
        if (Instance == null || Instance._catalogue == null) return 0;

        int total = 0;
        foreach (RecipeData r in Instance._catalogue.RecipesOfType(type))
            total += GetStock(r);
        return total;
    }

    /// <summary>Recetas de ese tipo que se han quedado a cero.</summary>
    public static int OutOfStockCountOfType(MinigameType type)
    {
        if (Instance == null || Instance._catalogue == null) return 0;

        int n = 0;
        foreach (RecipeData r in Instance._catalogue.RecipesOfType(type))
            if (GetStock(r) <= 0) n++;
        return n;
    }

    [ContextMenu("DEBUG: rellenar todo el stock")]
    private void DebugRefill()
    {
        if (_catalogue == null || OwnedItemsManager.Instance == null) return;
        foreach (RecipeData r in _catalogue.allRecipes)
            if (r != null) OwnedItemsManager.Instance.AddItem(Key(r), _catalogue.startingStockPerRecipe);
        OnStockChanged?.Invoke();
        Debug.Log("[IngredientStockManager] Stock rellenado (debug).");
    }
}
