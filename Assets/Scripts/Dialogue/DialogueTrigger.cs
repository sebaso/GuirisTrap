using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IReactionDelegate
{
    [SerializeField] 
    private Transform _reactions;
    
    private Queue<Reaction> _reactionQueue = new();
    private List<Reaction> _reactionsInExecution = new();
    private bool _isPlaying;

    public void TriggerDialogue()
    {
        if (_isPlaying) return;
        _isPlaying = true;

        _reactionQueue.Clear();
        foreach (var r in _reactions.GetComponentsInChildren<Reaction>())
        {
            r.Initialize(this);
            _reactionQueue.Enqueue(r);
        }
        NextReaction();
    }

    private void NextReaction()
    {
        if (_reactionQueue.Count > 0 && (_reactionsInExecution.Count == 0 || _reactionQueue.Peek().ExecuteDirectly))
        {
            Reaction r = _reactionQueue.Dequeue();
            r.ExecuteReaction();
            _reactionsInExecution.Add(r);
            NextReaction();
        }
    }

    public void OnReactionFinished(Reaction reaction)
    {
        _reactionsInExecution.Remove(reaction);
        if (_reactionQueue.Count == 0) _isPlaying = false;
        else NextReaction();
    }
}