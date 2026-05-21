using UnityEngine;

public class MinigameControllable : ControllableMonoBehaviour
{
    private IMinigameControllable _active;

    public void SetActive(IMinigameControllable controllable)
    {
        _active = controllable;
    }

    public void ClearActive()
    {
        _active = null;
    }

    public override void OnInteractDown()
    {
        _active?.OnInteract();
    }

    public override void OnMove(Vector2 direction)
    {
        _active?.OnNavigate(direction);
    }

    public override void OnCrouchDown()
    {
        //  Crouch es cancel temporalmente  
        _active?.OnCancel();
    }

    public override void OnAttackDown()
    {
        // Attack es submit temporalmente 
        _active?.OnSubmit();
    }
}