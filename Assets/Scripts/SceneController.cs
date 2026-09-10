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
            if (!CanStartDay())
            {
                HUDMessage.Instance?.ShowWarning("Hay sillas que no se pueden usar. Revisa el restaurante antes de empezar el día.");
                return;
            }
            SaveManager.Instance?.ForceSave();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private bool CanStartDay()
    {
        int totalTables = 0;
        int totalChairs = 0;

        foreach (GridZone zone in GridZone.ActiveZones)
        {
            if (zone.VoxelData == null) continue;

            totalTables += GridManager.CountByCategory(zone.VoxelData, PlaceableCategory.Table);
            totalChairs += GridManager.CountByCategory(zone.VoxelData, PlaceableCategory.Chair);

            foreach (var kvp in GridManager.ValidateAllChairs(zone.VoxelData))
                if (!kvp.Value) return false;
        }

        return totalTables > 0 && totalChairs > 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name == "MainMenu") return;

        if (scene.name == "PreparationScene")
        {
            MoneyManager.EnsureAndRestore();

            CameraController cameraController = FindAnyObjectByType<CameraController>();
            if (cameraController != null && cameraController.ActiveZone == null)
            {
                GridZone defaultZone = GridZone.ActiveZones.Find(z => z.ZoneId == ZoneId.Interior);
                if (defaultZone != null) cameraController.SetActiveZone(defaultZone);
            }

            bool isNewGame = SaveManager.Instance != null && SaveManager.Instance.ConsumePendingNewGame();

            if (isNewGame)
            {
                var upgradeManager = FindAnyObjectByType<ChiringuitoUpgradeManager>();
                if (upgradeManager != null)
                {
                    foreach (var grid in upgradeManager.GetAllTierGridData())
                        GridManager.ClearOccupied(grid);
                }
                else
                {
                    Debug.LogWarning("[SceneController] No se encontró ChiringuitoUpgradeManager en la escena, no se pudo limpiar el grid.");
                }
            }
            else if (SaveManager.Instance != null && SaveManager.Instance.ShouldSyncGridsOnLoad())
            {
                SaveManager.Instance.ApplyGridToScene();
                SaveManager.Instance.ApplyInventoryToScene(); 
            }

            PlaceableGenerator generator = FindAnyObjectByType<PlaceableGenerator>();
            generator?.Generate();
        }
        if (scene.name == "GameScene")
        {
            // El AudioManager persiste entre escenas: en GameScene la música la
            // llevan las AmbientZones, así que cortamos el tema anterior.
            AudioManager.Instance?.StopMusic();

            PlaceableGenerator generator = FindAnyObjectByType<PlaceableGenerator>();
            generator?.Generate();
        }
    }

    public bool IsSceneLoaded(string sceneName)
    {
        return sceneName == SceneManager.GetActiveScene().name;
    }
}