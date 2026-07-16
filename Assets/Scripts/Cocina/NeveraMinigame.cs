using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NeveraMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("UI References")]
    [SerializeField]
    private GameObject minigamePanel;
    [SerializeField]
    private TMP_Text timerText;

    [Header("Arrow Prefabs")]
    [SerializeField] private GameObject _arrowDefaultPrefab;
    [SerializeField] private GameObject _arrowSelectedPrefab;
    [SerializeField] private GameObject _arrowCorrectPrefab;
    [SerializeField] private Transform _arrowContainer;

    [Header("EGG Images")]
    [SerializeField] private Image _eggImage;
    [SerializeField] private Sprite _rawEgg;
    [SerializeField] private Sprite _coockedEgg;
    [SerializeField] private Sprite _burnedEgg;


    [Header("EGG Timers")]
    [SerializeField, Range(0f, 1f)] 
    private float _cookedThreshold = 0.66f;
    [SerializeField, Range(0f, 1f)] 
    private float _burntThreshold = 0.33f;

    private enum ArrowDir { Up, Down, Left, Right }

    private List<ArrowDir> currentSequence = new List<ArrowDir>();
    private List<GameObject> _arrowInstances = new List<GameObject>();

    private int currentIndex = 0;
    private bool isPlaying = false;
    private float timer;
    private PlayerController player;
    private RecipeData currentRecipe;

    // Cooldown para evitar que el stick registre múltiples inputs
    private float _inputCooldown = 0f;

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        player = currentPlayer;
        currentRecipe = recipe;

        InputManager.Instance.EnterMinigame(this);

        minigamePanel.SetActive(true);
        GenerateSequence(recipe.difficulty == 1 ? 4 : recipe.difficulty * 2 + 2);

        timer = recipe.timeLimit;
        currentIndex = 0;
        isPlaying = true;
        _inputCooldown = 0f;
        
        BuildArrowInstances();
        UpdateEggVisual();
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

        UpdateEggVisual();

        if(timer <= 0) EndGame(false);
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        minigamePanel.SetActive(false);
        InputManager.Instance.ExitMinigame();

        if (success)
        {
            MinigameFeedback.Show(true, $"¡{currentRecipe.dishName} listo!", "nevera_success");

            if (currentRecipe.foodPrefab != null)
                player.CreateAndHoldFood(currentRecipe.foodPrefab);
            else
                Debug.LogWarning($"[NeveraMinigame] {currentRecipe.dishName} no tiene foodPrefab.");
        }
        else
        {
            string reason = timer <= 0f ? "¡Se acabó el tiempo!" : "¡Secuencia incorrecta!";
            MinigameFeedback.Show(false, reason, "nevera_failure");
        }
    }
    // ------------------------------------------------------------------
    //  UI - Arrows
    // ------------------------------------------------------------------
 
    void BuildArrowInstances()
    {
        for (int i = _arrowInstances.Count - 1; i >= 0; i--)
            if (_arrowInstances[i] != null) Destroy(_arrowInstances[i]);
        _arrowInstances.Clear();
 
        for (int i = 0; i < currentSequence.Count; i++)
        {
            GameObject prefab = GetPrefabForState(i);
            GameObject instance = Instantiate(prefab, _arrowContainer);
            instance.transform.localRotation = Quaternion.Euler(0f, 0f, GetArrowRotationZ(currentSequence[i]));
            _arrowInstances.Add(instance);
        }
    }

    GameObject GetPrefabForState(int index)
    {
        if (index < currentIndex)  return _arrowCorrectPrefab;
        if (index == currentIndex) return _arrowSelectedPrefab;
        return _arrowDefaultPrefab;
    }

     void ReplaceArrowAt(int index)
    {
        if (index < 0 || index >= _arrowInstances.Count) return;
 
        GameObject old = _arrowInstances[index];
        int siblingIndex = old != null ? old.transform.GetSiblingIndex() : index;
        if (old != null) Destroy(old);
 
        GameObject prefab = GetPrefabForState(index);
        GameObject instance = Instantiate(prefab, _arrowContainer);
        instance.transform.SetSiblingIndex(siblingIndex); // mantiene el orden dentro del contenedor
        instance.transform.localRotation = Quaternion.Euler(0f, 0f, GetArrowRotationZ(currentSequence[index]));
 
        _arrowInstances[index] = instance;
    }

    float GetArrowRotationZ(ArrowDir dir) => dir switch
    {
        ArrowDir.Down  => 0f,
        ArrowDir.Right => 90f,
        ArrowDir.Up    => 180f,
        ArrowDir.Left  => 270f,
        _ => 0f
    };

    // ------------------------------------------------------------------
    //  UI - EGG
    // ------------------------------------------------------------------
 
    void UpdateEggVisual()
    {
        if (_eggImage == null || currentRecipe == null || currentRecipe.timeLimit <= 0f) return;
 
        float remainingRatio = Mathf.Clamp01(timer / currentRecipe.timeLimit);
 
        if (remainingRatio > _cookedThreshold)
            _eggImage.sprite = _rawEgg;
        else if (remainingRatio > _burntThreshold)
            _eggImage.sprite = _coockedEgg;
        else
            _eggImage.sprite = _burnedEgg;
    }
    public void OnInteract() { }
    public void OnSubmit() { }
    public void OnCancel() { }

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
            int resolvedIndex = currentIndex;
            currentIndex++;
 
            ReplaceArrowAt(resolvedIndex);
            if (currentIndex < currentSequence.Count)
                ReplaceArrowAt(currentIndex);
 
            if (currentIndex >= currentSequence.Count) EndGame(true);
        }
        else EndGame(false);
    }
}
