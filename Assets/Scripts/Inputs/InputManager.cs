using UnityEngine;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

public class InputManager : MonoBehaviour, IPlayerActions
{
    public static InputManager Instance { get; private set; }

    [Header("Controllables")]
    [SerializeField] private ControllableMonoBehaviour _playerControllable;
    [SerializeField] private MinigameControllable      _minigameControllable;

    private InputSystem_Actions _inputs;
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

    //  Cambio de contexto 

    /// Activa un minijuego/menú y pasa el control a MinigameControllable.
    public void EnterMinigame(IMinigameControllable minigame)
    {
        _minigameControllable.SetActive(minigame);
        _current = _minigameControllable;
    }

    /// Devuelve el control al jugador.
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

    //  Callbacks del Input System 

    public void OnMove(InputAction.CallbackContext context)
    {
        _current?.OnMove(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _current?.OnLook(context.ReadValue<Vector2>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed) _current?.OnInteractDown();
        else if (context.canceled) _current?.OnInteractUp();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed) _current?.OnAttackDown();
        else if (context.canceled) _current?.OnAttackUp();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed) _current?.OnCrouchDown();
        else if (context.canceled) _current?.OnCrouchUp();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) _current?.OnJumpDown();
        else if (context.canceled) _current?.OnJumpUp();
    }

    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context)     { }
    public void OnSprint(InputAction.CallbackContext context)   { }
}