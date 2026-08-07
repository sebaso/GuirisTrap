using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _pausePanel;
    [SerializeField] 
    private GameObject _continueButton;
    private bool _wasPaused;

    void Start()
    {
        UpdateContinueButtonVisibility();
    }

    void Update()
    {
        if (_pausePanel != null)
        {
            if ((SceneManager.GetActiveScene().name == "PreparationScene" || SceneManager.GetActiveScene().name == "GameScene" || SceneManager.GetActiveScene().name == "ZonaDePruebas") && Input.GetKeyDown(KeyCode.Escape))
            {
                if (!_pausePanel.activeInHierarchy)
                    _pausePanel.SetActive(true);
                else
                    _pausePanel.SetActive(false);
            }

            bool isPaused = _pausePanel.activeInHierarchy;
            if (isPaused != _wasPaused)
            {
                Time.timeScale = isPaused ? 0f : 1f;
                _wasPaused = isPaused;
            }
        }
    }
    public void OnClickButton(string sceneName)
    {
        Time.timeScale = 1f;
        SceneController.Instance.ChangeScene(sceneName);
    }
    public void OnClickNewGame()
    {
        SaveManager.Instance?.NewGame();
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    private void UpdateContinueButtonVisibility()
    {
            Debug.Log($"[EditorMenu] _continueButton asignado={_continueButton != null}, SaveManager.Instance={SaveManager.Instance != null}, HasSaveFile={SaveManager.Instance?.HasSaveFile}");

        if (_continueButton == null) return;
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile;
        _continueButton.SetActive(hasSave);
    }
}
