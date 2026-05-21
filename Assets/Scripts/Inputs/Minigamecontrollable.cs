using UnityEngine;

public class MinigameControllable : ControllableMonoBehaviour
{
    private IMinigameControllable _active;

    public void SetActive(IMinigameControllable controllable) => _active = controllable;
    public void ClearActive()                                 => _active = null;

    public override void OnInteractDown() => _active?.OnInteract();
    public override void OnCancelDown()   => _active?.OnCancel();
    public override void OnSubmitDown()   => _active?.OnSubmit();
    public override void OnMove(Vector2 direction) => _active?.OnNavigate(direction);
}