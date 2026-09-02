using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Peso de cada factor")]
    [SerializeField, Range(0f, 1f)] private float _dayWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _starsWeight = 0.4f;

    [Tooltip("Día en el que el factor de días llega a 1.")]
    [SerializeField] private float _daySaturatesAt = 30f;

    [Header("Clientes: intervalo de spawn (s)")]
    [SerializeField] private float _spawnIntervalStart = 14f;
    [SerializeField] private float _spawnIntervalEnd = 7f;

    [Header("Clientes: máximo simultáneo")]
    [SerializeField] private int _maxClientsStart = 6;
    [SerializeField] private int _maxClientsEnd = 14;

    [Header("Clientes: pesos tamaños grupo [1,2,3,4]")]
    [SerializeField] private float[] _groupWeightsStart = { 20f, 45f, 25f, 10f };
    [SerializeField] private float[] _groupWeightsEnd = { 5f, 30f, 35f, 30f };

    [Tooltip("Día jugado en el que se desbloquea cada tamaño de grupo [1,2,3,4]. " +
             "Antes de ese día ese tamaño no aparece (peso 0).")]
    [SerializeField] private int[] _groupSizeUnlockDays = { 1, 2, 4, 6 };

    [Header("Eventos (gaviotas, fuegos): multiplicador de probabilidad")]
    [SerializeField] private float _eventChanceStart = 0.5f;
    [SerializeField] private float _eventChanceEnd = 2f;

    [Header("Especiales (jefes)")]
    [SerializeField] private float _specialChanceStart = 0.5f;
    [SerializeField] private float _specialChanceEnd = 2.5f;
    [Tooltip("Especiales extra por día al llegar a dificultad máxima.")]
    [SerializeField] private int _extraSpecialsPerDayEnd = 2;
    [Tooltip("Día jugado a partir del cual pueden llegar especiales.")]
    [SerializeField] private int _minPlayingDayForSpecials = 3;

    private ClientSpawner _spawner;
    private bool _subscribedDay;

    public float Difficulty01 { get; private set; }

    public float SpawnInterval => Mathf.Lerp(_spawnIntervalStart, _spawnIntervalEnd, Difficulty01);
    public int MaxClients => Mathf.RoundToInt(Mathf.Lerp(_maxClientsStart, _maxClientsEnd, Difficulty01));
    public float EventMultiplier => Mathf.Lerp(_eventChanceStart, _eventChanceEnd, Difficulty01);
    public float SpecialMultiplier => Mathf.Lerp(_specialChanceStart, _specialChanceEnd, Difficulty01);
    public int ExtraSpecialsPerDay => Mathf.RoundToInt(_extraSpecialsPerDayEnd * Difficulty01);
    public bool SpecialsUnlocked
    {
        get
        {
            int playingDay = (SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0) + 1;
            return playingDay >= _minPlayingDayForSpecials;
        }
    }

    // Lecturas estáticas para los managers de eventos. Con DifficultyManager
    // ausente devuelven el comportamiento neutro (x1, sin especiales extra).
    public static float EventChanceScale => GetOrCreate().EventMultiplier;
    public static float SpecialChanceScale => GetOrCreate().SpecialMultiplier;
    public static int ExtraSpecials => GetOrCreate().ExtraSpecialsPerDay;
    public static bool SpecialsAllowed => GetOrCreate().SpecialsUnlocked;

    public static DifficultyManager GetOrCreate()
    {
        if (Instance == null)
            Instance = new GameObject("DifficultyManager").AddComponent<DifficultyManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (_groupWeightsStart == null || _groupWeightsStart.Length != 4 ||
            _groupWeightsEnd == null || _groupWeightsEnd.Length != 4)
        {
            Debug.LogWarning("[DifficultyManager] Los pesos de grupo deben tener 4 valores. Uso los por defecto.");
            _groupWeightsStart = new float[] { 20f, 45f, 25f, 10f };
            _groupWeightsEnd = new float[] { 5f, 30f, 35f, 30f };
        }

        if (_groupSizeUnlockDays == null || _groupSizeUnlockDays.Length != 4)
        {
            Debug.LogWarning("[DifficultyManager] _groupSizeUnlockDays debe tener 4 valores. Uso los por defecto.");
            _groupSizeUnlockDays = new int[] { 1, 2, 4, 6 };
        }
    }

    private void Start()
    {
        RefreshAndApply();
    }

    private void Update()
    {
        // DayManager puede no existir aún en el Awake (mismo patrón que DayReport).
        if (!_subscribedDay && DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += OnDayStarted;
            _subscribedDay = true;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_subscribedDay && DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= OnDayStarted;
    }

    private void OnDayStarted()
    {
        RefreshAndApply();
    }

    [ContextMenu("Recalcular y aplicar dificultad")]
    private void RefreshAndApply()
    {
        int day = SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0;
        float stars = SaveManager.Instance != null ? SaveManager.Instance.Stars : 0f;

        float dayT = _daySaturatesAt <= 0f ? 1f : Mathf.Clamp01(day / _daySaturatesAt);
        float starsT = Mathf.Clamp01(stars / 5f);
        Difficulty01 = Mathf.Clamp01(dayT * _dayWeight + starsT * _starsWeight);

        if (_spawner == null) _spawner = FindFirstObjectByType<ClientSpawner>();
        if (_spawner != null)
        {
            _spawner.spawnInterval = SpawnInterval;
            _spawner.maxClients = MaxClients;
            float[] weights = LerpWeights();
            ApplyGroupUnlocks(weights);
            _spawner.groupSizeWeights = weights;
        }

        Debug.Log($"[DifficultyManager] Día {day} ({stars:0.##}★) → dificultad {Difficulty01:0.00}: " +
                  $"spawn cada {SpawnInterval:0.#}s, máx {MaxClients} clientes, grupos hasta {MaxUnlockedGroupSize()}, " +
                  $"eventos x{EventMultiplier:0.##}, especiales x{SpecialMultiplier:0.##} (+{ExtraSpecialsPerDay}).");
    }

    // pone a 0 el peso de los tamaños aún no desbloqueados según el día jugado
    private void ApplyGroupUnlocks(float[] weights)
    {
        int playingDay = (SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0) + 1;
        int firstAvailable = -1;

        for (int i = 0; i < weights.Length; i++)
        {
            int unlockDay = Mathf.Max(1, _groupSizeUnlockDays[i]);
            if (playingDay < unlockDay)
                weights[i] = 0f;
            else if (firstAvailable < 0)
                firstAvailable = i;
        }

        // el calendario no puede dejar al spawner sin tamaños posibles
        if (firstAvailable < 0)
            weights[0] = 1f;
        else if (weights[firstAvailable] <= 0f)
            weights[firstAvailable] = 1f;
    }

    public int MaxUnlockedGroupSize()
    {
        int playingDay = (SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0) + 1;
        int max = 1;
        for (int i = 0; i < _groupSizeUnlockDays.Length; i++)
            if (playingDay >= Mathf.Max(1, _groupSizeUnlockDays[i]))
                max = i + 1;
        return max;
    }

    private float[] LerpWeights()
    {
        var w = new float[4];
        for (int i = 0; i < 4; i++)
            w[i] = Mathf.Lerp(_groupWeightsStart[i], _groupWeightsEnd[i], Difficulty01);
        return w;
    }
}
