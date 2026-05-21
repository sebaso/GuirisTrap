using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TMPEffects.Components;

public class DespensaMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("UI References")]
    public GameObject  minigamePanel;
    public Image       progressBarFill;
    public TMP_Text    timerText;
    public TMP_Text    mashText;
    public TMPAnimator mashAnimator;

    [Header("Settings")]
    public float baseClicks = 10f;

    private PlayerController player;
    private RecipeData       currentRecipe;
    private bool             isPlaying = false;
    private float            timer;
    private float            maxTimer;
    private float            currentClicks;
    private float            requiredClicks;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player        = currentPlayer;
        currentRecipe = recipe;

        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);

        requiredClicks = baseClicks + recipe.difficulty * 5;
        timer    = Mathf.Max(3f, recipe.timeLimit - recipe.difficulty * 0.5f);
        maxTimer = timer;

        currentClicks          = 0;
        progressBarFill.fillAmount = 0f;
        isPlaying              = true;

        if (mashAnimator) mashAnimator.ResetTime();
        if (mashText)     mashText.transform.localScale = Vector3.one;
        if (timerText)    timerText.color = Color.white;
    }

    void Update()
    {
        if (!isPlaying) return;

        timer -= Time.deltaTime;
        if (timerText)
        {
            timerText.text  = timer.ToString("F1");
            float ratio     = Mathf.Clamp01(timer / maxTimer);
            timerText.color = ratio > 0.5f
                ? Color.Lerp(new Color(1f, 0.6f, 0f), Color.white, (ratio - 0.5f) * 2f)
                : Color.Lerp(Color.red, new Color(1f, 0.6f, 0f), ratio * 2f);
        }

        if (timer <= 0) EndGame(false);
    }

    void LateUpdate()
    {
        if (mashText && isPlaying)
            mashText.transform.localScale = Vector3.Lerp(
                mashText.transform.localScale, Vector3.one, Time.deltaTime * 10f);
    }

    void AddProgress()
    {
        currentClicks++;
        progressBarFill.fillAmount = currentClicks / requiredClicks;
        if (mashText) mashText.transform.localScale = Vector3.one * 1.2f;
        if (currentClicks >= requiredClicks) EndGame(true);
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        minigamePanel.SetActive(false);
        if (timerText) timerText.color = Color.white;
        InputManager.Instance.ExitMinigame();

        if (success)
        {
            Debug.Log("¡Éxito!");
            if (currentRecipe.foodPrefab != null)
                Instantiate(currentRecipe.foodPrefab, player.transform.position, Quaternion.identity);
            else
                Debug.LogWarning($"[DespensaMinigame] {currentRecipe.dishName} no tiene foodPrefab.");
        }
        else Debug.Log("¡SE TE ACABÓ EL TIEMPO! PRINGAO");
    }

    //  IMinigameControllable

    public void OnNavigate(Vector2 direction) { }
    public void OnCancel()  { }
    public void OnSubmit()  => AddProgress();
    public void OnInteract() => AddProgress();
}