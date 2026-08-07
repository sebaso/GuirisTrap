using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

public class EditorMenu : MonoBehaviour, IPlayerActions
{
    [SerializeField]
    private GameObject _pausePanel;
    [SerializeField]
    private GameObject _continueButton;
    private bool _wasPaused;

    private InputSystem_Actions _inputs;

    void Awake()
    {
        _inputs = new InputSystem_Actions();
        _inputs.Player.AddCallbacks(this);
    }

    void OnEnable()  => _inputs.Player.Enable();
    void OnDisable() => _inputs.Player.Disable();
    void OnDestroy() { _inputs.Player.RemoveCallbacks(this); _inputs.Dispose(); }

    void Start()
    {
        UpdateContinueButtonVisibility();
    }

    private void UpdateContinueButtonVisibility()
    {
        if (_continueButton == null) return;
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile;
        _continueButton.SetActive(hasSave);
    }

    void Update()
    {
        if (_pausePanel != null)
        {
            bool isPaused = _pausePanel.activeInHierarchy;
            if (isPaused != _wasPaused)
            {
                Time.timeScale = isPaused ? 0f : 1f;
                _wasPaused = isPaused;
            }
        }
    }
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_pausePanel == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "PreparationScene" && sceneName != "GameScene" && sceneName != "ZonaDePruebas") return;

        _pausePanel.SetActive(!_pausePanel.activeInHierarchy);
    }

    public void OnMove(InputAction.CallbackContext context) { }
    public void OnLook(InputAction.CallbackContext context) { }
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnJump(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    public void OnSprint(InputAction.CallbackContext context) { }

    public void OnClickButton(string sceneName)
    {
        Time.timeScale = 1f;
        SceneController.Instance.ChangeScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OnClickNewGame()
    {
        SaveManager.Instance?.NewGame();
    }
}