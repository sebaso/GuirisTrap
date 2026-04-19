using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EspeciasMinigame : MonoBehaviour
{

    [Header("Interacción en Escena")]
    public float interactionDistance = 2.5f;

    [Header("Cuchara")]
    public float cucharaSpeed    = 5f;
    public float cucharaMinX     = -4f;
    public float cucharaMaxX     =  4f;
    public GameObject cucharaPrefab;   // PREFAB CUCHARA
    public Transform  cucharaSpawnPos; // POSICION DONDE APARECE LA CUCHARA 

    [Header("Bala")]
    public GameObject balaPrefab;
    public float      balaSpeed = 10f;

    [Header("UI - Panel")]
    public GameObject minigamePanel;
    public TMP_Text   balasText;
    public TMP_Text   instruccionText;
    public Image      resultadoImage; // Imagen que muestra éxito o fracaso al final

    private bool             _isPanelOpen  = false;
    private bool             _isPlaying    = false;
    private PlayerController _player;
    private RecipeData       _currentRecipe;

    private int              _balasRestantes;
    private int              _totalEspecias;
    private int              _especiasCongeladas;

    private EspecieroLayout  _layoutActivo;
    private GameObject       _cucharaObj;
    private float            _cucharaX;


    void Awake()
    {
        if (minigamePanel) minigamePanel.SetActive(false);
    }

    void Update()
    {
        if (!_isPanelOpen)
        {
            DetectPlayerInteraction();
            return;
        }

        if (!_isPlaying) return;

        HandleCucharaMovement();
        HandleShoot();
    }

    void DetectPlayerInteraction()
    {
        if (_player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj) _player = obj.GetComponent<PlayerController>();
        }
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.transform.position);
        if (dist <= interactionDistance && Input.GetKeyDown(KeyCode.E))
            Debug.Log("[EspeciasMinigame] Necesitas una receta de tipo Especias.");
    }

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        _currentRecipe = recipe;
        _player        = currentPlayer;
        _player.enabled = false;

        // Elegir layout aleatorio
        if (recipe.posiblesLayouts == null || recipe.posiblesLayouts.Length == 0)
        {
            Debug.LogWarning("[EspeciasMinigame] No hay layouts asignados en RecipeData.");
            _player.enabled = true;
            return;
        }

        GameObject layoutPrefab = recipe.posiblesLayouts[Random.Range(0, recipe.posiblesLayouts.Length)];
        Vector3 spawnPos = cucharaSpawnPos != null ? cucharaSpawnPos.position : transform.position + Vector3.up * 2f;
        GameObject layoutObj = Instantiate(layoutPrefab, spawnPos, Quaternion.identity);
        _layoutActivo = layoutObj.GetComponent<EspecieroLayout>();

        if (_layoutActivo == null)
        {
            Debug.LogError("[EspeciasMinigame] El layout no tiene EspecieroLayout.cs");
            Destroy(layoutObj);
            _player.enabled = true;
            return;
        }

        // ESCALADO DE VELOCIDAD SEGÚN DIFICULTAD
        float speedMultiplier = 1f + (recipe.difficulty - 1) * 0.3f;
        foreach (Especia e in _layoutActivo.GetEspecias())
            e.speed *= speedMultiplier;

        _totalEspecias      = _layoutActivo.GetEspecias().Length;
        _especiasCongeladas = 0;
        _balasRestantes     = recipe.balas;
        _cucharaX           = 0f;

        //  CUCHARA - INSTANTIAR Y POSICIONAR
        if (cucharaPrefab != null)
        {
            _cucharaObj = Instantiate(cucharaPrefab, spawnPos, Quaternion.identity);
        }

        minigamePanel.SetActive(true);
        _isPanelOpen = true;
        _isPlaying   = true;

        RefreshUI();
    }

    void HandleCucharaMovement()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h =  1f;

        _cucharaX = Mathf.Clamp(_cucharaX + h * cucharaSpeed * Time.deltaTime, cucharaMinX, cucharaMaxX);

        if (_cucharaObj != null)
        {
            Vector3 pos = _cucharaObj.transform.position;
            pos.x = _cucharaX;
            _cucharaObj.transform.position = pos;
        }
    }

    void HandleShoot()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_balasRestantes <= 0)
            {
                Debug.Log("[EspeciasMinigame] Sin balas.");
                return;
            }

            if (balaPrefab == null)
            {
                Debug.LogWarning("[EspeciasMinigame] Falta balaPrefab.");
                return;
            }

            Vector3 balaPos = _cucharaObj != null
                ? _cucharaObj.transform.position + Vector3.up * 0.5f
                : new Vector3(_cucharaX, cucharaSpawnPos.position.y, cucharaSpawnPos.position.z);

            GameObject balaObj = Instantiate(balaPrefab, balaPos, Quaternion.identity);
            Bala bala = balaObj.GetComponent<Bala>();
            if (bala != null)
            {
                bala.speed = balaSpeed;
                bala.Init(Vector3.up, this);
            }

            _balasRestantes--;
            RefreshUI();

            if (_balasRestantes <= 0)
                Invoke(nameof(CheckFinalSinBalas), 0.5f); 
        }
    }

    //  CALLBACKS DESDE Bala.cs
    public void OnBalaImpacta()
    {
        CheckVictoria();
    }

    public void OnEspeciaCongelada()
    {
        _especiasCongeladas++;
        RefreshUI();
        CheckVictoria();
    }

    void CheckVictoria()
    {
        if (_especiasCongeladas >= _totalEspecias)
            EndGame(true);
    }

    void CheckFinalSinBalas()
    {
        if (_isPlaying && _especiasCongeladas < _totalEspecias)
            EndGame(false);
    }

    void EndGame(bool success)
    {
        if (!_isPlaying) return;
        _isPlaying = false;

        // Limpiar layout y cuchara
        if (_layoutActivo != null) Destroy(_layoutActivo.gameObject);
        if (_cucharaObj   != null) Destroy(_cucharaObj);

        minigamePanel.SetActive(false);
        _isPanelOpen = false;
        _player.enabled = true;

        if (success)
        {
            Debug.Log($"[EspeciasMinigame] ¡OLEEEEEEE! {_currentRecipe.dishName}");
            if (_currentRecipe.foodPrefab != null)
                Instantiate(_currentRecipe.foodPrefab, _player.transform.position, Quaternion.identity);
            else
                Debug.LogWarning($"[EspeciasMinigame] {_currentRecipe.dishName} no tiene foodPrefab.");
        }
        else
        {
            Debug.Log("[EspeciasMinigame] Sin balas. CAGASTE!!!!");
        }
    }

    void RefreshUI()
    {
        if (balasText)
            balasText.text = $"Balas: {_balasRestantes}";

        if (instruccionText)
            instruccionText.text = "A/D Mover  |  E Disparar";
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}