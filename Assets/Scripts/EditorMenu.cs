using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _pausePanel;
    void Update()
    {
        // hasta que cambiemos de sistema de control, mantenemos esta porqueria
        if ((SceneManager.GetActiveScene().name == "PreparationScene" || SceneManager.GetActiveScene().name == "GameScene" || SceneManager.GetActiveScene().name == "ZonaDePruebas")  && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log(_pausePanel.activeInHierarchy);
            if(!_pausePanel.activeInHierarchy)
                _pausePanel.SetActive(true);
            else
                _pausePanel.SetActive(false);
        }
    }
    public void OnClickButton(string sceneName)
    {
        Debug.Log(sceneName);
        SceneController.Instance.ChangeScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
