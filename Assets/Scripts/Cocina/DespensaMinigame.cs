using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class DespensaMinigame : MonoBehaviour
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public Image progressBarFill;
    public TMP_Text timerText;
    public TMP_Text mashText; // Pon <rainbow>¡Aprieta E muchas veces!</rainbow> en el Inspector, no desde código

    [Header("Settings")]
    public float baseClicks = 10f;

    private PlayerController player;
    private RecipeData currentRecipe;
    private bool isPlaying = false;
    private float timer;
    private float maxTimer; // guardamos el tiempo máximo para calcular el ratio
    private float currentClicks;
    private float requiredClicks;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        currentRecipe = recipe;
        player.enabled = false;
        minigamePanel.SetActive(true);

        requiredClicks = baseClicks + (recipe.difficulty * 5);
        timer    = Mathf.Max(3f, recipe.timeLimit - (recipe.difficulty * 0.5f));
        maxTimer = timer;

        currentClicks = 0;
        progressBarFill.fillAmount = 0f;
        isPlaying = true;
        
        // NO tocamos mashText desde código — el <rainbow> está en el Inspector
        if(mashText) mashText.transform.localScale = Vector3.one;
        if(timerText) timerText.color = Color.white;
    }

    void Update()
    {
        if (!isPlaying) return;

        timer -= Time.deltaTime;

        if(timerText)
        {
            timerText.text = timer.ToString("F1");
            // Lerp de blanco a rojo según el tiempo restante
            float ratio = Mathf.Clamp01(timer / maxTimer);
            timerText.color = Color.Lerp(Color.red, Color.white, ratio);
        }

        if (timer <= 0) { EndGame(false); return; }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
            AddProgress();
    }

    void AddProgress()
    {
        currentClicks++;
        progressBarFill.fillAmount = currentClicks / requiredClicks;
        if(mashText) mashText.transform.localScale = Vector3.one * 1.2f;
        if (currentClicks >= requiredClicks) EndGame(true);
    }
    
    void LateUpdate()
    {
        if(mashText && isPlaying)
            mashText.transform.localScale = Vector3.Lerp(mashText.transform.localScale, Vector3.one, Time.deltaTime * 10f);
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        minigamePanel.SetActive(false);
        if(timerText) timerText.color = Color.white; // reset color
        player.enabled = true;
        
        if (success)
        {
            Debug.Log("¡Éxito!");
            if (currentRecipe.foodPrefab != null)
                Instantiate(currentRecipe.foodPrefab, player.transform.position, Quaternion.identity);
            else
                Debug.LogWarning($"[DespensaMinigame] {currentRecipe.dishName} no tiene foodPrefab asignado.");
        }
        else Debug.Log("¡SE TE ACABÓ EL TIEMPO! PRINGAO");
    }
}