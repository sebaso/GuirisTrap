using UnityEngine;

public class EditorMenu : MonoBehaviour
{

    public void OnClickButton(string sceneName)
    {
        SceneController.Instance.ChangeScene(sceneName);
    }
}
