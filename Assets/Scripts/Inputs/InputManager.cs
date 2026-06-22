using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static InputSystem_Actions;

public class InputManager : MonoBehaviour, IPlayerActions
{
    public static InputManager Instance { get; private set; }

    [Header("Controllables")]
    private ControllableMonoBehaviour _playerControllable;
    [SerializeField] private MinigameControllable      _minigameControllable;

    private InputSystem_Actions       _inputs;
    private ControllableMonoBehaviour _current;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _inputs = new InputSystem_Actions();
        _inputs.Enable();
        _inputs.Player.Enable();
        _inputs.Player.AddCallbacks(this);
        _inputs.UI.Disable();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name + " modo: " + mode);
        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (player != null)
        {
            Debug.Log("ENCUENTRA EL PLAYER");
            _playerControllable = player;
            _current = _playerControllable;
        }
        else
        {
                        Debug.Log("NOOOO ENCUENTRA EL PLAYER");

            _playerControllable = null;
            _current = null;
        }
    }
    public void EnterMinigame(IMinigameControllable minigame)
    {
        // Detiene al jugador para que no patine mientras juega el minijuego.
        (_playerControllable as PlayerController)?.LockMovement();

        _minigameControllable.SetActive(minigame);
        _current = _minigameControllable;
    }

    public void ExitMinigame()
    {
        _minigameControllable.ClearActive();
        _current = _playerControllable;

        (_playerControllable as PlayerController)?.UnlockMovement();
    }

    public void EnablePlayerInputs(bool value)
    {
        if (value) _inputs.Player.Enable();
        else       _inputs.Player.Disable();
    }

    //  Callbacks 

    public void OnMove(InputAction.CallbackContext context)
    {
            Debug.Log("OnMove: " + context.ReadValue<Vector2>() + " current: " + _current?.name);
_current?.OnMove(context.ReadValue<Vector2>());
    }

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