using System.Collections;
using UnityEngine;

public class Reaction : MonoBehaviour
{
    #if UNITY_EDITOR
        // Descripción de la reacción. Es una nota para el editor usando un string
        // en modo TextArea para mostrar un campo más grande y ver la información
        // de un vistazo.
        [SerializeField, TextArea]
        private string _description;
    #endif
    [SerializeField]
    [Tooltip("Indica si debe esperar a que finalice la reacción anterior.")]
    private bool _executeDirectly = false;
    public bool ExecuteDirectly => _executeDirectly;
    // Tiempo que tardará en ejecutarse la reacción
    [SerializeField]
    private float _duration;
    //private Interactable _interactable;
    private IReactionDelegate _delegate;
    // public void Initialize(Interactable interactable)
    // {
    //     _interactable = interactable;
    // }
    public void Initialize(IReactionDelegate @delegate)
    {
        _delegate = @delegate;
    }
    /// <summary>
    /// Acción a realizar antes del delay
    /// </summary>
    protected virtual void React(){}

    /// <summary>
    /// Acción a realizar tras el dealay
    /// </summary>
    protected virtual void PostReact()
    {
        //_interactable.NextReaction();
    }

    /// <summary>
    /// Coroutina que será ejecutada como reacción
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator ReactionCoroutine()
    {
        React();

        yield return new WaitForSeconds(_duration);

        PostReact();
        _delegate?.OnReactionFinished(this);
    }

    /// <summary>
    /// Método público para disparar la ejecución de la reacción
    /// </summary>
    public virtual void ExecuteReaction()
    {
        StartCoroutine(ReactionCoroutine());
    }

}
