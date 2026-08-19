using System.Collections.Generic;
using UnityEngine;


public class GaviotaEventManager : MonoBehaviour
{
    public static GaviotaEventManager Instance { get; private set; }

    [Header("Probabilidad")]
    [Tooltip("Segundos entre cada tirada de dado.")]
    [SerializeField] private float _rollInterval = 8f;
    [Tooltip("Probabilidad (0-1) de que caiga un regalito en cada tirada.")]
    [Range(0f, 1f)]
    [SerializeField] private float _cacaChancePerRoll = 0.2f;
    [Tooltip("Sin cacas antes de este segundo del día.")]
    [SerializeField] private float _minDelayBeforeFirst = 15f;
    [Tooltip("Máximo de cacas en el suelo a la vez.")]
    [SerializeField] private int _maxSimultaneas = 3;

    [Header("Zonas de caída")]
    [SerializeField] private Collider[] _dropZones;
    [Tooltip("Radio de caída alrededor del manager si no hay zonas asignadas.")]
    [SerializeField] private float _fallbackRadius = 6f;
    [Tooltip("Probabilidad de que el regalito apunte a la posición del jugador. " +
             "Es esquivable: la caída tarda unos instantes, sigue moviéndote.")]
    [Range(0f, 1f)]
    [SerializeField] private float _targetPlayerChance = 0.25f;

    [Header("Suelo")]
    [SerializeField] private LayerMask _groundLayers = ~0;
    [Tooltip("Altura del suelo si el rayo no encuentra nada (suelo sin collider).")]
    [SerializeField] private float _fallbackGroundY = 0f;
    [Tooltip("Cuánto se despega del suelo, para que no parpadee contra él.")]
    [SerializeField] private float _groundOffset = 0.02f;
    [Tooltip("Desde cuánto por encima del punto se lanza el rayo hacia abajo.")]
    [SerializeField] private float _rayStartHeight = 20f;
    [SerializeField] private float _rayLength = 60f;

    [Header("Caída")]
    [SerializeField] private float _dropHeight = 7f;
    [SerializeField] private float _fallTime = 0.5f;

    [Header("Visual")]
    [Tooltip("Prefab del regalito (con CacaGaviota + collider trigger). Vacío = placeholder.")]
    [SerializeField] private GameObject _cacaPrefab;

    private readonly List<CacaGaviota> _activas = new();
    private float _rollTimer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (DayManager.Instance == null || !DayManager.Instance.IsDayActive) return;
        float elapsed = DayManager.Instance.DayDuration - DayManager.Instance.TimeRemaining;
        if (elapsed < _minDelayBeforeFirst) return;

        _rollTimer += Time.deltaTime;
        if (_rollTimer < _rollInterval) return;
        _rollTimer = 0f;

        _activas.RemoveAll(c => c == null); // purgar las que ya murieron
        if (_activas.Count >= _maxSimultaneas) return;
        if (Random.value > _cacaChancePerRoll) return;

        SoltarCaca();
    }

    // ------------------------------------------------------------------

    private PlayerController _player;

    private void SoltarCaca()
    {
        // A veces la gaviota tiene puntería: apunta a donde estás AHORA.
        // Como la caída tarda _fallTime, moverse la esquiva (GDD: "pudiendo
        // caerte encima mientras llevas un plato").
        Vector3 point;
        if (Random.value < _targetPlayerChance && TryGetPlayer(out PlayerController pc))
            point = pc.transform.position;
        else
            point = RandomDropPoint();

        SoltarCacaEn(point);

        AudioManager.Instance?.PlaySFX("gaviota_graznido");
        HUDMessage.Instance?.ShowWarning("¡Una gaviota ha soltado un regalito!");
        Debug.Log("[GaviotaEventManager] Regalito entrante.");
    }


    private float BuscarSueloY(Vector3 point)
    {
        Vector3 origin = point + Vector3.up * _rayStartHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            origin, Vector3.down, _rayLength, _groundLayers, QueryTriggerInteraction.Ignore);

        float best = float.MinValue;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (h.collider.GetComponentInParent<CacaGaviota>() != null) continue;

            if (h.point.y > best) best = h.point.y;
        }

        if (best == float.MinValue)
        {
            Debug.LogWarning($"[GaviotaEventManager] No encuentro suelo bajo {point}. " +
                             $"Uso Fallback Ground Y = {_fallbackGroundY}. " +
                             "Revisa que el suelo tenga collider y esté en Ground Layers.");
            return _fallbackGroundY + _groundOffset;
        }

        return best + _groundOffset;
    }

    private bool TryGetPlayer(out PlayerController pc)
    {
        if (_player == null) _player = FindFirstObjectByType<PlayerController>();
        pc = _player;
        return pc != null;
    }

    public CacaGaviota SoltarCacaEn(Vector3 point)
    {
        point.y = BuscarSueloY(point);

        GameObject go = _cacaPrefab != null
            ? Instantiate(_cacaPrefab)
            : CreatePlaceholder();

        CacaGaviota caca = go.GetComponent<CacaGaviota>();
        if (caca == null) caca = go.AddComponent<CacaGaviota>();

        caca.IniciarCaida(point, _dropHeight, _fallTime);
        _activas.Add(caca);
        return caca;
    }

    private Vector3 RandomDropPoint()
    {
        Vector3 point;

        if (_dropZones != null && _dropZones.Length > 0)
        {
            Collider zone = _dropZones[Random.Range(0, _dropZones.Length)];
            Bounds b = zone.bounds;
            point = new Vector3(Random.Range(b.min.x, b.max.x),
                                b.center.y,
                                Random.Range(b.min.z, b.max.z));
        }
        else
        {
            Vector2 r = Random.insideUnitCircle * _fallbackRadius;
            point = transform.position + new Vector3(r.x, 0f, r.y);
        }

        return point; 
    }

    private GameObject CreatePlaceholder()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CacaGaviota (placeholder)";
        go.transform.localScale = new Vector3(0.5f, 0.12f, 0.5f);

        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(0.95f, 0.94f, 0.86f);

        SphereCollider col = go.GetComponent<SphereCollider>();
        if (col != null) { col.isTrigger = true; col.radius = 1.2f; }

        return go;
    }

    [ContextMenu("Forzar caca ahora")]
    private void DebugForceCaca() => SoltarCaca();
}