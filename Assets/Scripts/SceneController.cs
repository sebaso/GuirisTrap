using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{

    public static SceneController _instance;
    public static SceneController Instance => _instance;
    
    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeScene(string sceneName)
    {
        if(sceneName == "GameScene")
        {
            GameGridManager gridManager = FindFirstObjectByType<GameGridManager>();
            if(!gridManager.CanStartDay())
                return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameGridManager gridManager = FindFirstObjectByType<GameGridManager>();
        gridManager.Init();
        if (gridManager != null)
        {
            gridManager.PlaceableGenerator();
        }
    }
    public bool IsSceneLoaded(string sceneName)
    {
        if(sceneName == SceneManager.GetActiveScene().name)
            return true;
        return false;
    }
}
