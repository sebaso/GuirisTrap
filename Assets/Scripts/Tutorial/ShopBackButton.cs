using UnityEngine;

public class ShopBackButton : MonoBehaviour
{
    [SerializeField] 
    private GameObject _shopPanel;
    [SerializeField]
    private GameObject _mobiliaryPanel;
    [SerializeField] 
    private GameObject _decorationPanel;
    [SerializeField]
    private GameObject _foodPanel;
    [SerializeField] 
    private GameObject _HUD;
    public void OnBackToGameButton()
    {
        TutorialEvents.OnExitedShop?.Invoke();
        _shopPanel.SetActive(false);
        _mobiliaryPanel.SetActive(false);
        _decorationPanel.SetActive(false);
        _foodPanel.SetActive(false);
        _HUD.SetActive(true);
    }
}
