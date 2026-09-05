using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Paneles")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _optionsPanel;

    [Header("Comportamiento")]
    [SerializeField] private bool _allowPause = true;
    [SerializeField] private string _mainMenuScene = "MainMenu";

    [Header("Diagnóstico")]
    [SerializeField] private bool _debugLogs = false;

    public bool IsPaused { get; private set; }

    private int _lastInputFrame = -1;

    private InputSystem_Actions _inputs;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PauseMenuManager] Ya había un PauseMenuManager " +
                             $"('{Instance.name}'). Este ('{name}') se desactiva.", this);
            enabled = false;
            return;
        }

        Instance = this;
        SetPanels(pause: false, options: false, "Awake");

        _inputs = new InputSystem_Actions();
        _inputs.Player.Pause.performed += OnPauseAction;
    }

    void OnEnable()  => _inputs?.Player.Pause.Enable();
    void OnDisable() => _inputs?.Player.Pause.Disable();

    private void OnPauseAction(InputAction.CallbackContext ctx) => HandlePauseInput();

    void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = 1f;
            InputManager.Instance?.ExitPause();
        }
        if (Instance == this) Instance = null;

        if (_inputs != null)
        {
            _inputs.Player.Pause.performed -= OnPauseAction;
            _inputs.Dispose();
            _inputs = null;
        }
    }

    private void HandlePauseInput()
    {
        if (!_allowPause) return;

        if (_lastInputFrame == Time.frameCount)
        {
            if (_debugLogs)
                Debug.LogWarning("[PauseMenuManager] Input de pausa ignorado: ya se " +
                                 "había recibido en este frame.", this);
            return;
        }
        _lastInputFrame = Time.frameCount;

        if (_debugLogs)
            Debug.Log($"[PauseMenuManager] Input de pausa. IsPaused={IsPaused}, " +
                      $"opciones abiertas={(_optionsPanel != null && _optionsPanel.activeSelf)}", this);

        if (IsPaused && _optionsPanel != null && _optionsPanel.activeSelf)
        {
            CloseOptions();
            return;
        }

        if (IsPaused) Resume();
        else          Pause();
    }

    public void Pause()
    {
        if (IsPaused || !_allowPause) return;
        IsPaused = true;

        SetPanels(pause: true, options: false, "Pause");

        Time.timeScale = 0f;
        InputManager.Instance?.EnterPause();

        AudioManager.Instance?.PlaySFX("pause_open");
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        SetPanels(pause: false, options: false, "Resume");

        Time.timeScale = 1f;
        InputManager.Instance?.ExitPause();

        AudioManager.Instance?.PlaySFX("pause_close");
    }

    public void OpenOptions()
    {
        if (_optionsPanel == null) return;
        SetPanels(pause: false, options: true, "OpenOptions");
    }

    public void CloseOptions()
    {
        SetPanels(pause: IsPaused, options: false, "CloseOptions");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        InputManager.Instance?.ExitPause();

        SceneManager.LoadScene(_mainMenuScene);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPanels(bool pause, bool options, string origen)
    {
        Apply(_pausePanel, pause, "PausePanel", origen);
        Apply(_optionsPanel, options, "OptionsPanel", origen);
    }

    private void Apply(GameObject panel, bool value, string nombre, string origen)
    {
        if (panel == null) return;

        panel.SetActive(value);

        if (_debugLogs)
            Debug.Log($"[PauseMenuManager] {origen}: {nombre} → {(value ? "ON" : "OFF")}", this);
    }

    void LateUpdate()
    {
        if (!_debugLogs || !IsPaused || _pausePanel == null || _optionsPanel == null) return;

        if (!_pausePanel.activeSelf && !_optionsPanel.activeSelf)
            Debug.LogWarning("[PauseMenuManager] Estamos en pausa pero los dos paneles " +
                             "están apagados: alguien los ha apagado por fuera.", this);
    }
}
