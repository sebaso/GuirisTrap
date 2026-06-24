using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Button _button;

    private int _posX;
    private int _posY;

    private const float DISABLED_ALPHA = 0.35f;
    private const float ENABLED_ALPHA  = 1f;

    private PlaceableItemData _currentItem;

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
            _currentItem = null;
            return;
        }

        gameObject.SetActive(true);
        _currentItem = slot.item;

        if (_icon != null)
        {
            _icon.sprite = slot.item != null ? slot.item.icon : null;
            Color c = _icon.color;
            c.a = ENABLED_ALPHA;
            _icon.color = c;
        }

        if (_quantityText != null)
            _quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
    }

    public void RefreshCompatibility(PlaceableSurface activeSurface)
    {
        if (_currentItem == null || _icon == null) return;

        bool compatible = _currentItem.IsCompatibleWith(activeSurface);

        Color color = _icon.color;
        color.a = compatible ? ENABLED_ALPHA : DISABLED_ALPHA;
        _icon.color = color;

        if (_button != null)
            _button.interactable = compatible;
    }

    public void OnClick()
    {
        GameManager.Instance.Place(_posX, _posY);
    }
}
