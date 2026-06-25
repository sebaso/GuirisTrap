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
    private DialogueTrigger _part4;
    [SerializeField] 
    private DialogueTrigger _part5;
    private bool _tutorialActive = false;

    private const string TUTORIAL_KEY = "tutorial_done";
    private bool _boughtChair = false;
    private bool _boughtTable = false;

    void OnEnable()
    {
        TutorialEvents.OnItemBought += OnItemBought;
        TutorialEvents.OnEnteredShop += OnEnteredShop;
        TutorialEvents.OnEnteredForniture += OnEnteredFurniture;
        TutorialEvents.OnExitedShop += OnExitedShop;
        TutorialEvents.OnClosedInventory += OnCloseInventory;
    }

    void OnDisable()
    {
        TutorialEvents.OnItemBought -= OnItemBought;
        TutorialEvents.OnEnteredShop -= OnEnteredShop;
        TutorialEvents.OnEnteredForniture -= OnEnteredFurniture;
        TutorialEvents.OnExitedShop -= OnExitedShop;
        TutorialEvents.OnClosedInventory -= OnCloseInventory;
    }

    void Start()
    {
        if (!PlayerPrefs.HasKey(TUTORIAL_KEY))
        {
            _tutorialActive = true;
            StartCoroutine(LaunchTutorial());
        }
    }

    private void OnItemBought(PlaceableItemData item)
    {
        if (item.category == PlaceableCategory.Chair) _boughtChair = true;
        if (item.category == PlaceableCategory.Table) _boughtTable = true;
    }
    private IEnumerator LaunchTutorial()
    {
        yield return null;
        _part1.TriggerDialogue();
    }
    private void OnEnteredShop()
    {
        if (!_tutorialActive) return;
        _part2.TriggerDialogue();
    }

    private void OnEnteredFurniture()
    {
        if (!_tutorialActive) return;
        _part3.TriggerDialogue();
    }
    private void OnExitedShop()
    {
        if (!_tutorialActive) return;
        if (_boughtChair && _boughtTable)
            _part4.TriggerDialogue();
    }
    private void OnCloseInventory()
    {
        if (!_tutorialActive) return;
            _part5.TriggerDialogue();
            PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
            _tutorialActive = false;
    }
}