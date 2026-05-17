using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] 
    private Image _icon;
    [SerializeField] 
    private TMP_Text _quantityText;

    private int _posX;
    private int _posY;
    public void Init(int x, int y)
    {
        _posX = x;
        _posY = y;
    }
    public void SetSlot(InventorySlot slot)
    {
        if (slot == null)
        {
            gameObject.SetActive(false);
            _quantityText.text = "";
            return;
        }
        gameObject.SetActive(true);
        _icon.sprite = slot.item.icon;

        if(slot.quantity > 1)
            _quantityText.text = slot.quantity.ToString();
        else
            _quantityText.text = "";
    }
    public void OnClick()
    {
        //GameManager.Instance.Place(_posX, _posY);
    }
}