using UnityEngine;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

public class InputManager : MonoBehaviour, IPlayerActions
{
    public static InputManager Instance { get; private set; }

    [Header("Controllables")]
    [SerializeField] private ControllableMonoBehaviour _playerControllable;
    [SerializeField] private MinigameControllable      _minigameControllable;

    private InputSystem_Actions       _inputs;
    private ControllableMonoBehaviour _current;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _inputs = new InputSystem_Actions();
        _inputs.Enable();
        _inputs.Player.Enable();
        _inputs.Player.AddCallbacks(this);
        _inputs.UI.Disable();

        _current = _playerControllable;
    }

    public void EnterMinigame(IMinigameControllable minigame)
    {
        _minigameControllable.SetActive(minigame);
        _current = _minigameControllable;
    }

    public void ExitMinigame()
    {
        _minigameControllable.ClearActive();
        _current = _playerControllable;
    }

    public void EnablePlayerInputs(bool value)
    {
        if (value) _inputs.Player.Enable();
        else       _inputs.Player.Disable();
    }

    //  Callbacks 

    public void OnMove(InputAction.CallbackContext context)
        => _current?.OnMove(context.ReadValue<Vector2>());

    public void OnLook(InputAction.CallbackContext context)
        => _current?.OnLook(context.ReadValue<Vector2>());

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)      _current?.OnInteractDown();
        else if (context.canceled)  _current?.OnInteractUp();
    }

    // Cancel = Q 
    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed) _current?.OnCancelDown();
    }

    // Todavia no tienen nada... 
    public void OnAttack(InputAction.CallbackContext context)   { }
    public void OnCrouch(InputAction.CallbackContext context)   { }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)     _current?.OnJumpDown();
        else if (context.canceled) _current?.OnJumpUp();
    }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context)     { }
    public void OnSprint(InputAction.CallbackContext context)   { }
}