using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] 
    private PlaceableItemData _itemData;
    [SerializeField] 
    private Image _icon;
    [SerializeField] 
    private TMP_Text _priceText;
    [SerializeField] 
    private Button _button;

    void OnEnable()
    {
        UpgradeManager.OnItemUpgraded += HandleItemUpgraded;
        Refresh();
    }

    void OnDisable()
    {
        UpgradeManager.OnItemUpgraded -= HandleItemUpgraded;
    }

    private void HandleItemUpgraded(PlaceableItemData item, int newTierIndex)
    {
        if (item == _itemData) Refresh();
    }

    private void Refresh()
    {
        if (_itemData == null) return;

        int tierIndex = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTierIndex(_itemData) : 0;
        PlaceableTierData tier = _itemData.GetTier(tierIndex);
        if (tier == null) return;

        if (_icon != null) _icon.sprite = tier.icon;
        if (_priceText != null) _priceText.text = $"{tier.cost}€";
    }

    public void OnBuyClicked()
    {
        GameManager.Instance.Buy(_itemData);
    }
}