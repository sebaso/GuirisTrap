using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class DespensaMinigame : MonoBehaviour
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public Image progressBarFill;
    public TMP_Text timerText;    // Texto del tiempo restante
    public TMP_Text mashText;     // ¡PULSA ESPACIO! o lo que sea

    [Header("Settings")]
    public float baseClicks = 10f; // Clics necesarios en dificultad base 1

    private PlayerController player;
    private bool isPlaying = false;
    private float timer;
    private float currentClicks;
    private float requiredClicks;
    public GameObject food;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        player.enabled = false;
        minigamePanel.SetActive(true);

        requiredClicks = baseClicks + (recipe.difficulty * 5);
        
        timer = Mathf.Max(3f, recipe.timeLimit - (recipe.difficulty * 0.5f));

        currentClicks = 0;
        progressBarFill.fillAmount = 0f;
        isPlaying = true;
        
        if(mashText) mashText.transform.localScale = Vector3.one;
    }

    void Update()
    {
        if (!isPlaying) return;

        timer -= Time.deltaTime;
        if(timerText) timerText.text = timer.ToString("F1");

        if (timer <= 0)
        {
            EndGame(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            AddProgress();
        }
    }

    void AddProgress()
    {
        currentClicks++;
        progressBarFill.fillAmount = currentClicks / requiredClicks;

        if(mashText) 
            mashText.transform.localScale = Vector3.one * 1.2f;

        if (currentClicks >= requiredClicks)
        {
            EndGame(true);
        }
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
        player.enabled = true;
        
        if (success) {
            Debug.Log("¡Éxito!");
            Instantiate(food, player.transform.position, Quaternion.identity);
        }
        else Debug.Log("¡SE TE ACABÓ EL TIEMPO! PRINGAO");
    }
}