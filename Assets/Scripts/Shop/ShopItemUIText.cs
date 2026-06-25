using TMPro;
using UnityEngine;


public class ShopItemUIText : MonoBehaviour
{
    [SerializeField] 
    private TextMeshProUGUI countItemsText;
    [SerializeField] 
    private ShopUIManager shopUIManager;
    [SerializeField] 
    private PlaceableItemData item;

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
        countItemsText.text = shopUIManager.GetCountItem(item.name).ToString();
    }
}