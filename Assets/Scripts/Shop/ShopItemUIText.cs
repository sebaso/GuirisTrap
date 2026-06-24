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

    private void OnEnable()
    {
        PrintTextCountItems();
    }
   public void PrintTextCountItems()
    {
        countItemsText.text = shopUIManager.GetCountItem(item.name).ToString();
    }
}
