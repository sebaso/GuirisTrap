using UnityEngine;
using UnityEngine.UI;

public class ShopButtonLocker : MonoBehaviour
{
    [SerializeField] private Button _shopButton;
    [SerializeField] private CanvasGroup _canvasGroup;

    void OnEnable()
    {
        ShopEvents.OnShopButtonLocked += SetLocked;
    }

    void OnDisable()
    {
        ShopEvents.OnShopButtonLocked -= SetLocked;
    }

    private void SetLocked(bool locked)
    {
        _shopButton.interactable = !locked;
        _canvasGroup.alpha = locked ? 0.4f : 1f;
    }
}