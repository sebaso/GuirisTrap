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

        GameGridManager[] allManagers = FindObjectsByType<GameGridManager>(FindObjectsSortMode.None);

        foreach (GameGridManager manager in allManagers)
            manager.Init();

        if (SaveManager.Instance != null)
            SaveManager.Instance.LoadGrids(allManagers);

        foreach (GameGridManager manager in allManagers)
            manager.PlaceableGenerator();

        // Traspaso de dinero gameplay → escena de planificación: al llegar a la
        // PreparationScene garantizamos que el MoneyManager existe y carga el
        // saldo guardado en disco (lo que se ganó durante el día). Así el
        // "save" de fin de día se refleja en la escena de planificación aunque
        // no se haya pasado por MainMenu o el singleton se haya reiniciado.
        if (scene.name == "PreparationScene")
            MoneyManager.EnsureAndRestore();
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
