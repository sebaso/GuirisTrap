using System.Collections;
using UnityEngine;

public class TutorialStarter : MonoBehaviour
{
    [SerializeField] 
    private DialogueTrigger _part1;
    [SerializeField] 
    private DialogueTrigger _part2;
    [SerializeField] 
    private DialogueTrigger _part3;
    [SerializeField] 
    private GameObject _shopButtonBlinker;

    private const string TUTORIAL_KEY = "tutorial_done";
    private bool _boughtChair = false;
    private bool _boughtTable = false;

    void OnEnable()
    {
        ShopEvents.OnItemBought += OnItemBought;
        ShopEvents.OnEnteredShop += OnEnteredShop;
        ShopEvents.OnExitedShop += OnExitedShop;
    }

    void OnDisable()
    {
        ShopEvents.OnItemBought -= OnItemBought;
        ShopEvents.OnExitedShop -= OnExitedShop;
    }

    void Start()
    {
        if (!PlayerPrefs.HasKey(TUTORIAL_KEY))
            StartCoroutine(LaunchTutorial());
    }

    private IEnumerator LaunchTutorial()
    {
        yield return null;
        _part1.TriggerDialogue();
    }

    private void OnItemBought(PlaceableItemData item)
    {
        if (item.category == PlaceableCategory.Chair) _boughtChair = true;
        if (item.category == PlaceableCategory.Table) _boughtTable = true;
    }

    private void OnExitedShop()
    {
        if (_boughtChair && _boughtTable)
        {
            ShopButtonBlinker.OnStopBlink?.Invoke();
            _part3.TriggerDialogue();
            PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
        }
    }
    private void OnEnteredShop()
    {
        ShopButtonBlinker.OnStopBlink?.Invoke();
        _part2.TriggerDialogue();
    }
}