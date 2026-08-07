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
            GridManager gm = FindAnyObjectByType<GridManager>();
            if (gm != null && !gm.CanStartDay())
            {
                HUDMessage.Instance?.ShowWarning("Hay sillas que no se pueden usar. Revisa el restaurante antes de empezar el día.");
                return;
            }
            SaveManager.Instance?.ForceSave();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name == "MainMenu") return;

        if (scene.name == "PreparationScene")
        {
            MoneyManager.EnsureAndRestore();

            GridManager gridManager = FindAnyObjectByType<GridManager>();
            Debug.Log($"[SceneController] gridManager encontrado = {gridManager != null}");

            bool isNewGame = SaveManager.Instance != null && SaveManager.Instance.ConsumePendingNewGame();
            Debug.Log($"[SceneController] isNewGame = {isNewGame}");

            if (isNewGame)
            {
                gridManager?.ClearAll();
                Debug.Log("[SceneController] ClearAll ejecutado");
            }
            else if (SaveManager.Instance != null && SaveManager.Instance.ShouldSyncGridsOnLoad())
            {
                SaveManager.Instance.ApplyGridToScene(gridManager);
                Debug.Log("[SceneController] ApplyGridToScene ejecutado");
            }

            PlaceableGenerator generator = FindAnyObjectByType<PlaceableGenerator>();
            generator?.Generate();
        }
    }

    public bool IsSceneLoaded(string sceneName)
    {
        return sceneName == SceneManager.GetActiveScene().name;
    }
}