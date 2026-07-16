using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CongeladorMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public RectTransform cursorRect;
    public RectTransform targetZone;

    [Header("Ajustes de Dificultad Base")]
    [Tooltip("Velocidad base de la barra. Más bajo = más lento y fácil de leer. Feedback del profe: iba muy rápida.")]
    public float baseSpeed = 0.8f;
    [Tooltip("Cuánto se acelera por nivel de dificultad. 1 = sin cambio; 1.1 = +10% por nivel.")]
    public float speedMultiplierPerLevel = 1.1f;
    public float baseZoneSize = 0.3f;
    public float cursorWidth = 10f;

    private float currentSpeed;
    private float winMin, winMax;
    private bool isPlaying = false;
    private float timeElapsed;
    private float inputCooldown;
    private PlayerController player;
    private RecipeData currentRecipe;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        currentRecipe = recipe;

        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);

        currentSpeed = baseSpeed * Mathf.Pow(speedMultiplierPerLevel, recipe.difficulty - 1);
        float zoneSize = Mathf.Max(baseZoneSize * Mathf.Pow(0.85f, recipe.difficulty - 1), 0.05f);
        float randomPos = Random.Range(0.1f + zoneSize / 2, 0.9f - zoneSize / 2);
        winMin = randomPos - zoneSize / 2;
        winMax = randomPos + zoneSize / 2;

        targetZone.anchorMin = new Vector2(winMin, 0);
        targetZone.anchorMax = new Vector2(winMax, 1);
        targetZone.offsetMin = Vector2.zero;
        targetZone.offsetMax = Vector2.zero;

        inputCooldown = 0.5f;
        isPlaying = true;
        timeElapsed = 0f;
    }

    void Update()
    {
        if (!isPlaying) return;

        if (inputCooldown > 0) inputCooldown -= Time.deltaTime;

        timeElapsed += Time.deltaTime * currentSpeed;
        float currentPos = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(timeElapsed, 1f));

        cursorRect.anchorMin = new Vector2(currentPos, 0);
        cursorRect.anchorMax = new Vector2(currentPos, 1);
        cursorRect.anchoredPosition = Vector2.zero;
        cursorRect.sizeDelta = new Vector2(cursorWidth, 0);
    }

    void CheckWin(float finalPos)
    {
        isPlaying = false;
        if (finalPos >= winMin && finalPos <= winMax)
        {
            Debug.Log($"¡CONGELADO PERFECTO!");
            AudioManager.Instance?.PlaySFX("congelador_success");
            if (currentRecipe.foodPrefab != null)
                player.CreateAndHoldFood(currentRecipe.foodPrefab);
            else
                Debug.LogWarning($"[CongeladorMinigame] {currentRecipe.dishName} no tiene foodPrefab.");
        }
        else
        {
            {
                HUDMessage.Instance?.ShowBad("Fallaste el congelado.");
            }
            AudioManager.Instance?.PlaySFX("congelador_failure");
        }

        minigamePanel.SetActive(false);
        InputManager.Instance.ExitMinigame();
    }

    //  IMinigameControllable 

    public void OnNavigate(Vector2 direction) { }
    public void OnCancel() { }
    public void OnSubmit() => OnInteract();

    public void OnInteract()
    {
        if (!isPlaying || inputCooldown > 0) return;
        float currentPos = Mathf.SmoothStep(0f, 1f,
            Mathf.PingPong(timeElapsed, 1f));
        CheckWin(currentPos);
    }
}