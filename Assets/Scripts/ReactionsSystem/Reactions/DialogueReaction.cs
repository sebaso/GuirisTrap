using System;
using System.Collections;
using UnityEngine;

public class DialogueReaction : Reaction
{
    public static Action<string, Color, Sprite> OnDialogueReactionStart;
    public static Action OnDialogueReactionFinish;
    [SerializeField]
    private string _textKey;
    [SerializeField]
    private Color _color;
    [SerializeField]
    private Sprite _portrait;
    protected override void React()
    {
        base.React();
        string text = TranslateManager.Instance.GetTextWithKey(_textKey);
        OnDialogueReactionStart?.Invoke(text, _color, _portrait);
    }
    protected override void PostReact()
    {
        base.PostReact();
        OnDialogueReactionFinish?.Invoke();
    }

    // El diálogo espera a que el jugador lo avance en vez de esperar _duration.
    public override void ExecuteReaction()
    {
        StartCoroutine(DialogueCoroutine());
    }

    private IEnumerator DialogueCoroutine()
    {
        React();

        if (DialogueManager.Instance != null)
            yield return DialogueManager.Instance.WaitForAdvance();

        PostReact();
        Delegate?.OnReactionFinished(this);
    }
}
