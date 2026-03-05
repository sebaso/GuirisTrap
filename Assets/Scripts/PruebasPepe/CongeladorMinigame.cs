using UnityEngine;
using UnityEngine.UI; // Necesario para UI normal
using TMPro; 

public class CongeladorMinigame : MonoBehaviour
{
[Header("UI References")]
    public GameObject minigamePanel;
    public RectTransform cursorRect;
    public RectTransform targetZone;

    [Header("Ajustes de Dificultad Base")]
    [Tooltip("Velocidad en Nivel 1")]
    public float baseSpeed = 1.5f; 
    
    [Tooltip("Cuánto aumenta la velocidad por cada nivel de dificultad (1.2 = +20%)")]
    public float speedMultiplierPerLevel = 1.2f;

    [Tooltip("Ancho de la zona verde en Nivel 1 (0.1 a 0.5 recomendado)")]
    public float baseZoneSize = 0.3f;

    [Tooltip("Ancho visual de la aguja blanca")]
    public float cursorWidth = 10f;

    private float currentSpeed;
    private float winMin;
    private float winMax;
    private bool isPlaying = false;
    private float timeElapsed;
    private float inputCooldown;
    private PlayerController player;    
    public GameObject food;

public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        player.enabled = false;
        minigamePanel.SetActive(true);
   
        currentSpeed = baseSpeed * Mathf.Pow(speedMultiplierPerLevel, recipe.difficulty - 1);

        float zoneSize = baseZoneSize * Mathf.Pow(0.85f, recipe.difficulty - 1);
        zoneSize = Mathf.Max(zoneSize, 0.05f); 

        float randomPos = Random.Range(0.1f + (zoneSize/2), 0.9f - (zoneSize/2));
        winMin = randomPos - (zoneSize / 2);
        winMax = randomPos + (zoneSize / 2);

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

        if (inputCooldown > 0) 
        {
            inputCooldown -= Time.deltaTime;
        }
        

        timeElapsed += Time.deltaTime * currentSpeed;

        float rawPingPong = Mathf.PingPong(timeElapsed, 1f);
        float currentPos = Mathf.SmoothStep(0f, 1f, rawPingPong);

        cursorRect.anchorMin = new Vector2(currentPos, 0);
        cursorRect.anchorMax = new Vector2(currentPos, 1);
        cursorRect.anchoredPosition = Vector2.zero; 

        cursorRect.sizeDelta = new Vector2(cursorWidth, 0); 

        if (inputCooldown <= 0 && (Input.GetKeyDown(KeyCode.Space) 
                                || Input.GetKeyDown(KeyCode.Return) 
                                || Input.GetKeyDown(KeyCode.E)))
        {
            CheckWin(currentPos);
        }
    }
    void CheckWin(float finalPos)
    {
        isPlaying = false;
        
        // COMPROBAMOS SI ESTÁ DENTRO DE LA ZONA VERDE
        if (finalPos >= winMin && finalPos <= winMax)
        {
            Debug.Log($"¡CONGELADO PERFECTO! Pos: {finalPos} (Target: {winMin}-{winMax})");
            Instantiate(food, player.transform.position, Quaternion.identity);
            EndGame(true);
        }
        else
        {
            Debug.Log($"FALLASTE. Pos: {finalPos} (Target: {winMin}-{winMax})");
            EndGame(false);
        }
    }

    void EndGame(bool success)
    {
        minigamePanel.SetActive(false);
        player.enabled = true;
    }
}