using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _pausePanel;
    private bool _wasPaused;

    void Update()
    {
        // hasta que cambiemos de sistema de control, mantenemos esta porqueria
        if ((SceneManager.GetActiveScene().name == "PreparationScene" || SceneManager.GetActiveScene().name == "GameScene" || SceneManager.GetActiveScene().name == "ZonaDePruebas")  && Input.GetKeyDown(KeyCode.Escape))
        {
            if(!_pausePanel.activeInHierarchy)
                _pausePanel.SetActive(true);
            else
                _pausePanel.SetActive(false);
        }

        // Se sincroniza con el estado real del panel (no solo con la tecla Escape)
        // porque el botón de "Reanudar" del propio panel lo cierra con un
        // SetActive(false) directo desde el UnityEvent del Canvas, sin pasar por aquí.
        bool isPaused = _pausePanel.activeInHierarchy;
        if (isPaused != _wasPaused)
        {
            Time.timeScale = isPaused ? 0f : 1f;
            _wasPaused = isPaused;
        }
    }
    public void OnClickButton(string sceneName)
    {
        Time.timeScale = 1f;
        SceneController.Instance.ChangeScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
