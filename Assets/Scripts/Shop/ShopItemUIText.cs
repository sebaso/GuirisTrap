using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ShopItemUIText : MonoBehaviour
{
    [SerializeField] 
    private TextMeshProUGUI countItemsText;
    [SerializeField] 
    private ShopUIManager shopUIManager;
    [SerializeField] 
    private PlaceableItemData item;

    [Header("Límite de unidades")]
    [SerializeField] private Button buyButton;
    [SerializeField] private string maxReachedLabel = "MÁX";

    void OnEnable()
    {
        TutorialEvents.OnItemBought += OnItemBought;
        PrintTextCountItems();
    }

    void OnDisable()
    {
        TutorialEvents.OnItemBought -= OnItemBought;
    }

    private void OnItemBought(PlaceableItemData boughtItem)
    {
        if (boughtItem.name == item.name)
            PrintTextCountItems();
    }

    public void PrintTextCountItems()
    {
        int owned = shopUIManager.GetCountItem(item.name);
        bool maxed = item.maxStack > 0 && owned >= item.maxStack;

        // Con límite se ve "2/2"; sin límite, solo la cantidad como antes.
        countItemsText.text = item.maxStack > 0
            ? (maxed ? $"{owned}/{item.maxStack}  {maxReachedLabel}" : $"{owned}/{item.maxStack}")
            : owned.ToString();

        if (buyButton != null) buyButton.interactable = !maxed;
    }
}