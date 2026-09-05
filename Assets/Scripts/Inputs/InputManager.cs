using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static InputSystem_Actions;

public class InputManager : MonoBehaviour, IPlayerActions
{
    public static InputManager Instance { get; private set; }

    [Header("Controllables")]
    private ControllableMonoBehaviour _playerControllable;
    private MinigameControllable _minigameControllable;
    private DialogueControllable _dialogueControllable;

    private InputSystem_Actions _inputs;
    private ControllableMonoBehaviour _current;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        DontDestroyOnLoad(gameObject);

        _dialogueControllable = gameObject.AddComponent<DialogueControllable>();

        _inputs = new InputSystem_Actions();
        _inputs.Enable();
        _inputs.Player.Enable();
        _inputs.Player.AddCallbacks(this);
        _inputs.UI.Disable();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        _playerControllable = player;

        _minigameControllable = FindAnyObjectByType<MinigameControllable>();

        _current = _playerControllable;
        IsPaused = false;
        _beforePause = null;
    }
    private void OnDestroy()
{
    SceneManager.sceneLoaded -= OnSceneLoaded;

    if (_inputs != null)
    {
        _inputs.Disable();
        _inputs.Dispose();
    }
}
public void EnterMinigame(IMinigameControllable minigame)
{
    if (_minigameControllable == null)
    {
        Debug.LogError("No existe un MinigameControllable en esta escena.");
        return;
    }

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

    public void EnterDialogue()
    {
        (_playerControllable as PlayerController)?.LockMovement();
        _current = _dialogueControllable;
    }

    public void ExitDialogue()
    {
        _current = _playerControllable;
        (_playerControllable as PlayerController)?.UnlockMovement();
    }

    public void EnablePlayerInputs(bool value)
    {
        if (value) _inputs.Player.Enable();
        else       _inputs.Player.Disable();
    }


    public void OnMove(InputAction.CallbackContext context)
    {
        _current?.OnMove(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
        => _current?.OnLook(context.ReadValue<Vector2>());

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)      _current?.OnInteractDown();
        else if (context.canceled)  _current?.OnInteractUp();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed) _current?.OnCancelDown();
    }

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

    public void OnPause(InputAction.CallbackContext context)    {}


    private ControllableMonoBehaviour _beforePause;
    public bool IsPaused { get; private set; }

    public void EnterPause()
    {
        if (IsPaused) return;
        IsPaused = true;

        _beforePause = _current;
        _current = null;
        (_playerControllable as PlayerController)?.LockMovement();
    }

    public void ExitPause()
    {
        if (!IsPaused) return;
        IsPaused = false;

        _current = _beforePause;
        _beforePause = null;

        if (_current == _playerControllable)
            (_playerControllable as PlayerController)?.UnlockMovement();
    }
}