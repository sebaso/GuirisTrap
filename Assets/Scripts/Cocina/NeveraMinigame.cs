using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NeveraMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public TMP_Text   sequenceDisplay;
    public TMP_Text   timerText;

    private enum ArrowDir { Up, Down, Left, Right }

    private List<ArrowDir> currentSequence = new List<ArrowDir>();
    private int              currentIndex   = 0;
    private bool             isPlaying      = false;
    private float            timer;
    private PlayerController player;
    private RecipeData       currentRecipe;

    // Cooldown para evitar que el stick registre múltiples inputs
    private float _inputCooldown = 0f;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player        = currentPlayer;
        currentRecipe = recipe;

        InputManager.Instance.EnterMinigame(this);

        minigamePanel.SetActive(true);
        GenerateSequence(recipe.difficulty == 1 ? 4 : recipe.difficulty * 2 + 2);

        timer        = recipe.timeLimit;
        currentIndex = 0;
        isPlaying    = true;
        _inputCooldown = 0f;

        UpdateUI();
    }

    void GenerateSequence(int length)
    {
        currentSequence.Clear();
        for (int i = 0; i < length; i++)
            currentSequence.Add((ArrowDir)Random.Range(0, 4));
    }

    void Update()
    {
        if (!isPlaying) return;
        if (_inputCooldown > 0f) _inputCooldown -= Time.deltaTime;

        timer -= Time.deltaTime;
        timerText.text = timer.ToString("F1");
        if (timer <= 0) EndGame(false);
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        minigamePanel.SetActive(false);
        InputManager.Instance.ExitMinigame();

        if (success)
        {
            Debug.Log("¡Éxito!");
            if (currentRecipe.foodPrefab != null)
                Instantiate(currentRecipe.foodPrefab, player.transform.position, Quaternion.identity);
            else
                Debug.LogWarning($"[NeveraMinigame] {currentRecipe.dishName} no tiene foodPrefab.");
        }
        else Debug.Log("¡Fallo!");
    }

    void UpdateUI()
    {
        string display = "";
        for (int i = 0; i < currentSequence.Count; i++)
        {
            string symbol = GetArrowSymbol(currentSequence[i]);
            if      (i < currentIndex)  display += $"<color=green>{symbol} </color>";
            else if (i == currentIndex) display += $"<color=yellow><b><grow>[{symbol}]</grow></b></color> ";
            else                        display += $"{symbol} ";
        }
        sequenceDisplay.text = display;
    }

    string GetArrowSymbol(ArrowDir dir) => dir switch
    {
        ArrowDir.Up    => "↑",
        ArrowDir.Down  => "↓",
        ArrowDir.Left  => "←",
        ArrowDir.Right => "→",
        _              => "?"
    };

    public void OnInteract() { } 
    public void OnSubmit()   { }
    public void OnCancel()   { } 

    public void OnNavigate(Vector2 direction)
    {
        if (!isPlaying || _inputCooldown > 0f) return;
        if (Mathf.Abs(direction.x) < 0.5f && Mathf.Abs(direction.y) < 0.5f) return;

        ArrowDir pressed;
        if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            pressed = direction.y > 0 ? ArrowDir.Up : ArrowDir.Down;
        else
            pressed = direction.x > 0 ? ArrowDir.Right : ArrowDir.Left;

        _inputCooldown = 0.2f;

        if (pressed == currentSequence[currentIndex])
        {
            currentIndex++;
            if (currentIndex >= currentSequence.Count) EndGame(true);
            else UpdateUI();
        }
        else EndGame(false);
    }
}