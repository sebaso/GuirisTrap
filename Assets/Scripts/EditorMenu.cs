using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorMenu : MonoBehaviour
{

    public void OnClickButton(string sceneName)
    {
        SceneController.Instance.ChangeScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
