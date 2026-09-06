using System.Collections;
using UnityEngine;

public class ShopOpenButton : MonoBehaviour
{
    [SerializeField] 
    private GameObject _shopPanel;
    [SerializeField] 
    private GameObject _screenOff;
    [SerializeField] 
    private GameObject _mobileMenu;

    [SerializeField] 
    private int _blinkCount = 3;
    [SerializeField] 
    private float _blinkInterval = 0.15f;

    public void OnOpenShopButton()
    {
        TutorialEvents.OnEnteredShop?.Invoke();
        _shopPanel.SetActive(true);
        StartCoroutine(BlinkScreenOn());
    }

    private IEnumerator BlinkScreenOn()
    {
        _mobileMenu.SetActive(true);
        _screenOff.SetActive(true);

        for (int i = 0; i < _blinkCount; i++)
        {
            yield return new WaitForSeconds(_blinkInterval);
            _screenOff.SetActive(false);

            yield return new WaitForSeconds(_blinkInterval);
            _screenOff.SetActive(true);
        }

        _screenOff.SetActive(false);
    }
}