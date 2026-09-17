using UnityEngine;

public class ShopBackButton : MonoBehaviour
{
    [SerializeField] 
    private GameObject _shopPanel;
    [SerializeField]
    private GameObject _mobiliaryPanel;
    [SerializeField] 
    private GameObject _screenOff;
    [SerializeField]
    private GameObject _foodPanel;
    [SerializeField] 
    private GameObject _HUD;
    [SerializeField]
    private GameObject _mobileMenu;
    [SerializeField]
    private TutorialStarter _tutorialStarter;
    private bool _aux = false;
    public void OnBackToGameButton()
    {
        if(_tutorialStarter.GetItemsBought() && _aux == false)
        {
            TutorialEvents.OnExitedShop?.Invoke();
            _aux = true;
        }
        _shopPanel.SetActive(false);
        _mobiliaryPanel.SetActive(false);
        _screenOff.SetActive(false);
        _foodPanel.SetActive(false);
        _HUD.SetActive(true);
    }
    public void BackToMainMenuShopButton()
    {
        _mobiliaryPanel.SetActive(false);
        _foodPanel.SetActive(false);
        _mobileMenu.SetActive(true);
    }
}
