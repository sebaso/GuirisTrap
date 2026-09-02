using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class IngredientShopItemUI : MonoBehaviour
{
    [Header("Qué pack vende")]
    [Tooltip("Índice del pack dentro de la lista 'Packs' del IngredientCatalogue.")]
    [SerializeField] private int _packIndex = 0;

    [Header("Textos (opcionales)")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _stockText;

    [Header("Visual (opcional)")]
    [SerializeField] private Image _iconImage;
    [Tooltip("Se pone en rojo cuando alguna receta de este pack está a cero.")]
    [SerializeField] private Color _outOfStockColor = new Color(1f, 0.42f, 0.42f);
    [Tooltip("Color del cartel GRATIS de la primera compra.")]
    [SerializeField] private Color _freeColor = new Color(0.35f, 0.85f, 0.4f);

    private Color _stockBaseColor = Color.white;

    void Awake()
    {
        if (_stockText != null) _stockBaseColor = _stockText.color;
    }

    void OnEnable()
    {
        IngredientStockManager.OnStockChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        IngredientStockManager.OnStockChanged -= Refresh;
    }

    private static string ColorToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    private IngredientCatalogue.Pack GetPack()
    {
        IngredientCatalogue cat = IngredientStockManager.Instance != null
            ? IngredientStockManager.Instance.Catalogue
            : null;

        if (cat == null || cat.packs == null) return null;
        if (_packIndex < 0 || _packIndex >= cat.packs.Count) return null;
        return cat.packs[_packIndex];
    }

    public void Refresh()
    {
        IngredientCatalogue.Pack pack = GetPack();
        if (pack == null)
        {
            if (_nameText != null) _nameText.text = "(pack no encontrado)";
            return;
        }

        if (_nameText != null)  _nameText.text = pack.displayName;
        if (_iconImage != null && pack.icon != null) _iconImage.sprite = pack.icon;

        if (_priceText != null)
        {
            bool free = IngredientStockManager.IsPackFree(pack);
            _priceText.text = free
                ? $"<color={ColorToHex(_freeColor)}>GRATIS</color>  <size=70%><s>{pack.price}\u20AC</s></size>"
                : $"{pack.price}\u20AC";
        }

        if (_stockText != null)
        {
            _stockText.text = IngredientStockManager.StockBreakdownOfType(pack.type, ColorToHex(_outOfStockColor));
            _stockText.color = _stockBaseColor;
        }
    }

    /// <summary>Engánchalo al On Click () del botón.</summary>
    public void Buy()
    {
        IngredientCatalogue.Pack pack = GetPack();
        if (pack == null)
        {
            Debug.LogWarning("[IngredientShopItemUI] Pack no encontrado: revisa el Pack Index y el catálogo.");
            return;
        }

        if (IngredientStockManager.TryBuyPack(pack))
            Refresh();
    }
}
