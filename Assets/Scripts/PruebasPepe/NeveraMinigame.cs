using UnityEngine;
using TMPro; 
using System.Collections.Generic;

public class NeveraMinigame : MonoBehaviour
{
    [Header("UI References")]
    public GameObject minigamePanel; 
    public TMP_Text sequenceDisplay; 
    public TMP_Text timerText;
    public GameObject food;    
    
    [Header("Settings")]
    public bool allowWASD = true;

    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool isPlaying = false;
    private float timer;
    private PlayerController player; 
    private KeyCode[] arrowKeys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        player.enabled = false; 
        
        minigamePanel.SetActive(true);
        GenerateSequence(recipe.difficulty == 1 ? 4 : recipe.difficulty * 2 + 2); 
        
        timer = recipe.timeLimit;
        currentIndex = 0;
        isPlaying = true;
        
        UpdateUI();
    }

    void GenerateSequence(int length)
    {
        currentSequence.Clear();
        for (int i = 0; i < length; i++)
        {
            currentSequence.Add(arrowKeys[Random.Range(0, arrowKeys.Length)]);
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        timer -= Time.deltaTime;
        timerText.text = timer.ToString("F1");
        
        if (timer <= 0)
        {
            EndGame(false);
            return;
        }

        if (Input.anyKeyDown)
        {
            KeyCode requiredKey = currentSequence[currentIndex];

            if (IsCorrectKey(requiredKey))
            {
                currentIndex++;
                if (currentIndex >= currentSequence.Count) EndGame(true);
                else UpdateUI();
            }
            else
            {
                if (IsAnyControlKeyDown())
                {
                    EndGame(false);
                }
            }
        }
    }

    bool IsCorrectKey(KeyCode arrow)
    {
        if (Input.GetKeyDown(arrow)) return true;

        if (allowWASD)
        {
            if (arrow == KeyCode.UpArrow && Input.GetKeyDown(KeyCode.W)) return true;
            if (arrow == KeyCode.DownArrow && Input.GetKeyDown(KeyCode.S)) return true;
            if (arrow == KeyCode.LeftArrow && Input.GetKeyDown(KeyCode.A)) return true;
            if (arrow == KeyCode.RightArrow && Input.GetKeyDown(KeyCode.D)) return true;
        }
        return false;
    }

    bool IsAnyControlKeyDown()
    {
        foreach (KeyCode k in arrowKeys) if (Input.GetKeyDown(k)) return true;
        if (allowWASD)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || 
                Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) return true;
        }
        return false;
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
        else Debug.Log("¡Fallo!");
    }

    void UpdateUI()
    {
        string display = "";
        for (int i = 0; i < currentSequence.Count; i++)
        {
            string arrowSymbol = GetArrowSymbol(currentSequence[i]);

            if (i < currentIndex) 
                display += $"<color=green>{arrowSymbol} </color>";
            else if (i == currentIndex) 
                display += $"<color=yellow><b>[{arrowSymbol}]</b></color> ";
            else 
                display += $"{arrowSymbol} ";
        }
        sequenceDisplay.text = display;
    }

    string GetArrowSymbol(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            default: return key.ToString();
        }
    }
}