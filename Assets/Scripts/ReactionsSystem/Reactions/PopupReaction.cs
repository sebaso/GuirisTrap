using System.Collections;
using UnityEngine;

public class PopupReaction : Reaction
{
    [SerializeField]
    private PopupController _popup;
    [SerializeField]
    private string _textKey;

    private bool _popupClosed;

    protected override void React()
    {
        base.React();
        string text = TranslateManager.Instance.GetTextWithKey(_textKey);
        _popupClosed = false;
        _popup.Show(text, OnPopupClosed);
    }

    private void OnPopupClosed()
    {
        _popupClosed = true;
    }

    public override void ExecuteReaction()
    {
        StartCoroutine(PopupCoroutine());
    }

    private IEnumerator PopupCoroutine()
    {
        React();

        while (!_popupClosed)
            yield return null;

        PostReact();
        Delegate?.OnReactionFinished(this);
    }
}