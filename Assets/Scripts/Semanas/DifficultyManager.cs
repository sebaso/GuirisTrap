using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Peso de cada factor")]
    [SerializeField, Range(0f, 1f)] private float _dayWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _starsWeight = 0.4f;

    [Tooltip("Día en el que el factor de días llega a 1.")]
    [SerializeField] private float _daySaturatesAt = 60f;

    [Header("Clientes: intervalo de spawn (s)")]
    [SerializeField] private float _spawnIntervalStart = 14f;
    [SerializeField] private float _spawnIntervalEnd = 10f;

    [Header("Clientes: máximo simultáneo")]
    [SerializeField] private int _maxClientsStart = 6;
    [SerializeField] private int _maxClientsEnd = 12;

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
    private DifficultyParams _params;
    private DifficultySnapshot _snapshot;
    private bool _hasSnapshot;

    public float Difficulty01 => Snapshot().difficulty01;

    public float SpawnInterval => Snapshot().spawnInterval;
    public int MaxClients => Snapshot().maxClients;
    public float EventMultiplier => Snapshot().eventMultiplier;
    public float SpecialMultiplier => Snapshot().specialMultiplier;
    public int ExtraSpecialsPerDay => Snapshot().extraSpecialsPerDay;
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

        _params = BuildParams();
    }

    private DifficultyParams BuildParams() => new DifficultyParams
    {
        dayWeight = _dayWeight,
        starsWeight = _starsWeight,
        daySaturatesAt = _daySaturatesAt,
        spawnIntervalStart = _spawnIntervalStart,
        spawnIntervalEnd = _spawnIntervalEnd,
        maxClientsStart = _maxClientsStart,
        maxClientsEnd = _maxClientsEnd,
        groupWeightsStart = _groupWeightsStart,
        groupWeightsEnd = _groupWeightsEnd,
        groupSizeUnlockDays = _groupSizeUnlockDays,
        eventChanceStart = _eventChanceStart,
        eventChanceEnd = _eventChanceEnd,
        specialChanceStart = _specialChanceStart,
        specialChanceEnd = _specialChanceEnd,
        extraSpecialsPerDayEnd = _extraSpecialsPerDayEnd,
        minPlayingDayForSpecials = _minPlayingDayForSpecials,
    };

    private DifficultySnapshot Snapshot()
    {
        if (!_hasSnapshot)
        {
            if (_params == null) _params = BuildParams();
            int day = SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0;
            float stars = SaveManager.Instance != null ? SaveManager.Instance.Stars : 0f;
            _snapshot = DifficultyCurve.Evaluate(_params, day, stars);
            _hasSnapshot = true;
        }
        return _snapshot;
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
    public void RefreshAndApply()
    {
        _params = BuildParams();
        int day = SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0;
        float stars = SaveManager.Instance != null ? SaveManager.Instance.Stars : 0f;
        _snapshot = DifficultyCurve.Evaluate(_params, day, stars);
        _hasSnapshot = true;

        if (_spawner == null) _spawner = FindAnyObjectByType<ClientSpawner>();
        if (_spawner != null)
        {
            _spawner.spawnInterval = _snapshot.spawnInterval;
            _spawner.maxClients = _snapshot.maxClients;
            _spawner.groupSizeWeights = _snapshot.groupWeights;
        }

        Debug.Log($"[DifficultyManager] Día {day} ({stars:0.##}★) → dificultad {_snapshot.difficulty01:0.00}: " +
                  $"spawn cada {SpawnInterval:0.#}s, máx {MaxClients} clientes, grupos hasta {MaxUnlockedGroupSize()}, " +
                  $"eventos x{EventMultiplier:0.##}, especiales x{SpecialMultiplier:0.##} (+{ExtraSpecialsPerDay}).");
    }

    public int MaxUnlockedGroupSize()
    {
        int playingDay = (SaveManager.Instance != null ? SaveManager.Instance.CurrentDay : 0) + 1;
        return DifficultyCurve.MaxUnlockedGroupSize(playingDay, _groupSizeUnlockDays);
    }
}
