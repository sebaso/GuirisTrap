using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController _instance;
    public static SceneController Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
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
        if (sceneName == "GameScene")
        {
            GameGridManager floorManager = GetFloorManager();
            if (floorManager != null && !floorManager.CanStartDay())
                return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name == "MainMenu") return;

        // Inicializar TODOS los managers
        GameGridManager[] allManagers = FindObjectsByType<GameGridManager>(FindObjectsSortMode.None);

        foreach (GameGridManager manager in allManagers)
        {
            manager.Init();
            manager.PlaceableGenerator();
        }
    }

    public bool IsSceneLoaded(string sceneName)
    {
        return sceneName == SceneManager.GetActiveScene().name;
    }

    private GameGridManager GetFloorManager()
    {
        GameGridManager[] allManagers = FindObjectsByType<GameGridManager>(FindObjectsSortMode.None);
        foreach (GameGridManager manager in allManagers)
        {
            if (manager.Surface == PlaceableSurface.Floor)
                return manager;
        }
        return null;
    }
}
