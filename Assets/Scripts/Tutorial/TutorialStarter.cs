using System.Collections;
using UnityEngine;

public class TutorialStarter : MonoBehaviour
{
    [Header("Tutorial")]
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
    [SerializeField]
    private DialogueTrigger _popup1;
    [SerializeField]
    private DialogueTrigger _popup2;
    [SerializeField]
    private PlaceableItemData  _chair;
    [SerializeField]
    private PlaceableItemData  _table;

    [Header("Day 2")]
    [SerializeField]
    private DialogueTrigger _day2;
    [Header("Day 3")]
    [SerializeField]
    private DialogueTrigger _day3;
    [Header("Day 4")]
    [SerializeField]
    private DialogueTrigger _day4;
    [Header("Day 5")]
    [SerializeField]
    private DialogueTrigger _day5;
    [Header("Day 6")]
    [SerializeField]
    private DialogueTrigger _day6;
    [Header("Day 7")]
    [SerializeField]
    private DialogueTrigger _day7;

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
        TutorialEvents.OnEnteredFoodShop += OnEnteredFoodShop;
        TutorialEvents.OnInventoryEntered += OnInventoryEntered;
    }

    void OnDisable()
    {
        TutorialEvents.OnItemBought -= OnItemBought;
        TutorialEvents.OnEnteredShop -= OnEnteredShop;
        TutorialEvents.OnEnteredForniture -= OnEnteredFurniture;
        TutorialEvents.OnExitedShop -= OnExitedShop;
        TutorialEvents.OnEnteredFoodShop -= OnEnteredFoodShop;
        TutorialEvents.OnInventoryEntered -= OnInventoryEntered;
    }

    void Start()
    {
        int days = SaveManager.Instance.CurrentDay;

        if(days == 0)
        {
            _tutorialActive = true;
            StartCoroutine(LaunchTutorial());
        }else if(days == 1)
        {
            StartCoroutine(LaunchDay2Dialogues());
        }else if(days == 2)
        {
            StartCoroutine(LaunchDay3Dialogues());
        }else if(days == 3)
        {
            StartCoroutine(LaunchDay4Dialogues());
        }else if(days == 4)
        {
            StartCoroutine(LaunchDay5Dialogues());
        }else if(days == 5)
        {
            StartCoroutine(LaunchDay6Dialogues());
        }else if(days == 6)
        {
            StartCoroutine(LaunchDay7Dialogues());
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
    private IEnumerator LaunchDay2Dialogues()
    {
        yield return null;
        _day2.TriggerDialogue();
    }
    private IEnumerator LaunchDay3Dialogues()
    {
        yield return null;
        _day3.TriggerDialogue();
    }
    private IEnumerator LaunchDay4Dialogues()
    {
        yield return null;
        _day4.TriggerDialogue();
    }
    private IEnumerator LaunchDay5Dialogues()
    {
        yield return null;
        _day5.TriggerDialogue();
    }
    private IEnumerator LaunchDay6Dialogues()
    {
        yield return null;
        _day6.TriggerDialogue();
    }
    private IEnumerator LaunchDay7Dialogues()
    {
        yield return null;
        _day7.TriggerDialogue();
    }
    private void OnEnteredShop()
    {
        if (!_tutorialActive) return;
        _part2.TriggerDialogue();
        _chair.maxStack = 2;
        _table.maxStack = 2;
    }

    private void OnEnteredFurniture()
    {
        if (!_tutorialActive) return;
        _part3.TriggerDialogue();
    }
    private void OnEnteredFoodShop()
    {
        if (!_tutorialActive) return;
        _part5.TriggerDialogue();
    }
    private void OnExitedShop()
    {
        if (!_tutorialActive) return;
        if (_boughtChair && _boughtTable)
        {
            _part4.TriggerDialogue();
            _popup1.TriggerDialogue();
        }
    }
    private void OnInventoryEntered()
    {
        if (!_tutorialActive) return;
        _popup2.TriggerDialogue();
        _tutorialActive = false;
    }
}