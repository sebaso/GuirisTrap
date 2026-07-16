using UnityEngine;
using TMPro;
using TMPEffects.Components;
using UnityEngine.UI;

public class DespensaMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("UI References")]
    [SerializeField]
    private GameObject minigamePanel;
    [SerializeField]
    private TMP_Text timerText;
    [SerializeField]
    private TMP_Text mashText;
    [SerializeField]
    private TMPAnimator mashAnimator;
    private float _progress = 0f;

    [Header("Settings")]
    [SerializeField]
    private float baseClicks = 10f;
    [SerializeField]
    private float countdownSeconds = 3f;

    [Header("Tomatoes")]
    [SerializeField]
    private Image _tomatoeImage;
    [SerializeField]
    private Sprite _tomatoeSprite;
    [SerializeField]
    private Sprite _tomatoeSprite2;
    [SerializeField]
    private Sprite _tomatoeSprite3;
    [SerializeField]
    private Sprite _tomatoeSprite4;
    [SerializeField]
    private Sprite _tomatoeSprite5;
    [SerializeField]
    private Sprite _tomatoeSprite6;
    [SerializeField]
    private Sprite _tomatoeSprite7;
    [SerializeField]
    private Sprite _tomatoeSprite8;
    private PlayerController player;
    private RecipeData currentRecipe;
    private bool isPlaying = false;
    private bool isCountingDown = false;
    private float countdownRemaining;
    private float timer;
    private float maxTimer;
    private float currentClicks;
    private float requiredClicks;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        currentRecipe = recipe;

        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);

        requiredClicks = baseClicks + recipe.difficulty * 5;
        timer = Mathf.Max(3f, recipe.timeLimit - recipe.difficulty * 0.5f);
        maxTimer = timer;

        currentClicks = 0;
        _progress = 0f;

        if (mashAnimator) mashAnimator.ResetTime();
        if (mashText) mashText.transform.localScale = Vector3.one;
        if (timerText) timerText.color = Color.white;

        // Empezar con la cuenta atrás, NO jugando todavía. Da margen al jugador
        // para entender el minijuego antes de que corra el tiempo (feedback del profe).
        isPlaying          = false;
        isCountingDown     = true;
        countdownRemaining = countdownSeconds;
    }

    void Update()
    {
        if(_progress <= 0.125f)
        {
            _tomatoeImage.sprite = _tomatoeSprite;
        }else if(_progress > 0.125f && _progress <= 0.25f)
        {
            _tomatoeImage.sprite = _tomatoeSprite2;
        }else if (_progress > 0.25f && _progress <= 0.375f)
        {
            _tomatoeImage.sprite = _tomatoeSprite3;
        }else if (_progress > 0.375f && _progress <= 0.5f)
        {
            _tomatoeImage.sprite = _tomatoeSprite4;
        }else if (_progress > 0.5f && _progress <= 0.625f)
        {
            _tomatoeImage.sprite = _tomatoeSprite5;
        }else if (_progress > 0.625f && _progress <= 0.75f)
        {
            _tomatoeImage.sprite = _tomatoeSprite6;
        }else if (_progress > 0.75f && _progress <= 0.875f)
        {
            _tomatoeImage.sprite = _tomatoeSprite7;
        }else if (_progress > 875f && _progress <= 1f)
        {
            _tomatoeImage.sprite = _tomatoeSprite8;
        }
        // Fase de cuenta atrás: 3, 2, 1, ¡YA!
        if (isCountingDown)
        {
            countdownRemaining -= Time.deltaTime;

            if (timerText)
            {
                if (countdownRemaining > 0f)
                {
                    timerText.text  = Mathf.CeilToInt(countdownRemaining).ToString();
                    timerText.color = Color.white;
                }
                else
                {
                    timerText.text = "¡YA!";
                }
            }

            if (countdownRemaining <= 0f)
            {
                isCountingDown = false;
                isPlaying      = true; // ahora sí empieza a contar el tiempo
            }
            return;
        }

        if (!isPlaying) return;

        timer -= Time.deltaTime;
        if (timerText)
        {
            timerText.text = timer.ToString("F1");
            float ratio = Mathf.Clamp01(timer / maxTimer);
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
        if (!isPlaying) return; // ignorar pulsaciones durante la cuenta atrás

        currentClicks++;
        _progress = currentClicks / requiredClicks;

        if (mashText) mashText.transform.localScale = Vector3.one * 1.2f;
        if (currentClicks >= requiredClicks) EndGame(true);
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        isCountingDown = false;
        minigamePanel.SetActive(false);
        if (timerText) timerText.color = Color.white;
        InputManager.Instance.ExitMinigame();

        if (success)
        {
            MinigameFeedback.Show(true, $"¡{currentRecipe.dishName} listo!", "despensa_success");

            if (currentRecipe.foodPrefab != null)
                player.CreateAndHoldFood(currentRecipe.foodPrefab);
            else
                Debug.LogWarning($"[DespensaMinigame] {currentRecipe.dishName} no tiene foodPrefab.");
        }
        else
        {
            MinigameFeedback.Show(false, "¡Se acabó el tiempo!", "despensa_failure");
        }
    }

    //  IMinigameControllable

    public void OnNavigate(Vector2 direction) { }
    public void OnCancel() { }
    public void OnSubmit() => AddProgress();
    public void OnInteract() => AddProgress();
}
