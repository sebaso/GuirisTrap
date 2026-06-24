using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] 
    private GameObject _dialoguePanel;
    [SerializeField] 
    private TextMeshProUGUI _dialogueText;
    [SerializeField] 
    private Image _portraitImage;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _dialoguePanel.SetActive(false);
    }

    void OnEnable()
    {
        DialogueReaction.OnDialogueReactionStart += ShowDialogue;
        DialogueReaction.OnDialogueReactionFinish += HideDialogue;
    }

    void OnDisable()
    {
        DialogueReaction.OnDialogueReactionStart -= ShowDialogue;
        DialogueReaction.OnDialogueReactionFinish -= HideDialogue;
    }

    private void ShowDialogue(string text, Color color, Sprite portrait)
    {
        _dialoguePanel.SetActive(true);
        _dialogueText.text = text;
        _dialogueText.color = color;
        
        if (portrait != null)
        {
            _portraitImage.sprite = portrait;
            _portraitImage.gameObject.SetActive(true);
        }
        else
        {
            _portraitImage.gameObject.SetActive(false);
        }
    }
    private void HideDialogue()
    {
        _dialoguePanel.SetActive(false);
    }
}