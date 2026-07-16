using System.Collections;
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

    public bool IsShowingDialogue { get; private set; }
    private bool _advanceRequested;

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

        IsShowingDialogue = true;
        InputManager.Instance?.EnterDialogue();
    }
    private void HideDialogue()
    {
        _dialoguePanel.SetActive(false);
        IsShowingDialogue = false;
        InputManager.Instance?.ExitDialogue();
    }

    /// <summary>Llamado por el input del jugador (ver DialogueControllable) para avanzar el diálogo.</summary>
    public void RequestAdvance()
    {
        if (IsShowingDialogue) _advanceRequested = true;
    }

    /// <summary>Usado por DialogueReaction para esperar a que el jugador avance en vez de un tiempo fijo.</summary>
    public IEnumerator WaitForAdvance()
    {
        _advanceRequested = false;
        // deja pasar un frame para no consumir con la misma pulsación que abrió esta línea
        yield return null;
        while (!_advanceRequested)
            yield return null;
    }
}