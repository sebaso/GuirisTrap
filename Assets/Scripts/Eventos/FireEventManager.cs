using System.Collections.Generic;
using UnityEngine;


public class FireEventManager : MonoBehaviour
{
    public static FireEventManager Instance { get; private set; }

    [Header("Minijuego")]
    [SerializeField] private IncendioMinigame _incendioMinigame;

    [Header("Probabilidad")]
    [SerializeField] private float _rollInterval = 10f;
    [Range(0f, 1f)]
    [SerializeField] private float _fireChancePerRoll = 0.15f;
    [SerializeField] private float _minDelayBeforeFirstFire = 20f;
    [SerializeField] private int _maxSimultaneousFires = 1;

    [Header("Visual del fuego")]
    [SerializeField] private GameObject _fireVfxPrefab;
    [SerializeField] private Vector3 _fireVfxOffset = new Vector3(0f, 1.2f, 0f);

    /// <summary>Estación ardiendo → instancia de su VFX de fuego.</summary>
    private readonly Dictionary<CookingStation, GameObject> _burning = new();

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
        // Solo hay incendios con el día en marcha y pasado el margen inicial.
        if (DayManager.Instance == null || !DayManager.Instance.IsDayActive) return;
        float elapsed = DayManager.Instance.DayDuration - DayManager.Instance.TimeRemaining;
        if (elapsed < _minDelayBeforeFirstFire) return;

        _rollTimer += Time.deltaTime;
        if (_rollTimer < _rollInterval) return;
        _rollTimer = 0f;

        if (_burning.Count >= _maxSimultaneousFires) return;
        if (Random.value > _fireChancePerRoll) return;

        IgniteRandomStation();
    }


    /// <summary>¿Está ardiendo esta estación? (La consulta CookingStation.TryInteract.)</summary>
    public bool IsBurning(CookingStation station)
        => station != null && _burning.ContainsKey(station);

    /// <summary>Lanza el minijuego de apagar el fuego de una estación en llamas.</summary>
    public void StartExtinguishMinigame(CookingStation station, PlayerController player)
    {
        if (!IsBurning(station)) return;

        if (_incendioMinigame == null)
        {
            Debug.LogError("[FireEventManager] IncendioMinigame no asignado en el Inspector.");
            return;
        }

        _incendioMinigame.StartMinigame(station, player);
    }

    /// <summary>Prende una estación concreta (también usable desde otros eventos/días con ID).</summary>
    public void Ignite(CookingStation station)
    {
        if (station == null || IsBurning(station)) return;

        GameObject vfx = CreateFireVfx(station.transform);
        _burning.Add(station, vfx);

        AudioManager.Instance?.PlaySFX("fire_start");
        HUDMessage.Instance?.ShowWarning($"¡FUEGO en la estación de {station.stationType}!");
        Debug.Log($"[FireEventManager] ¡Incendio en {station.name} ({station.stationType})!");
    }

    /// <summary>Apaga el fuego de una estación (lo llama IncendioMinigame al ganar).</summary>
    public void Extinguish(CookingStation station)
    {
        if (station == null || !_burning.TryGetValue(station, out GameObject vfx)) return;

        if (vfx != null) Destroy(vfx);
        _burning.Remove(station);

        Debug.Log($"[FireEventManager] Fuego apagado en {station.name}.");
    }

    // ------------------------------------------------------------------
    //  Interno
    // ------------------------------------------------------------------

    private void IgniteRandomStation()
    {
        CookingStation[] all = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);

        // Candidatas: estaciones que no estén ya ardiendo.
        List<CookingStation> candidates = new();
        foreach (CookingStation s in all)
            if (!IsBurning(s)) candidates.Add(s);

        if (candidates.Count == 0) return;

        Ignite(candidates[Random.Range(0, candidates.Count)]);
    }

    private GameObject CreateFireVfx(Transform station)
    {
        GameObject vfx;

        if (_fireVfxPrefab != null)
        {
            vfx = Instantiate(_fireVfxPrefab, station);
            vfx.transform.localPosition = _fireVfxOffset;
            return vfx;
        }

        // Sin prefab de arte: placeholder. Si el shader de llama procedural
        // está en el proyecto, quad con fuego animado; si no, esfera naranja.
        Shader fireShader = Shader.Find("Guiri/Fire");

        if (fireShader != null)
        {
            vfx = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vfx.name = "FirePlaceholder (Guiri/Fire)";
            Object.Destroy(vfx.GetComponent<Collider>());
            vfx.transform.SetParent(station);
            vfx.transform.localPosition = _fireVfxOffset;
            vfx.transform.localScale = new Vector3(0.9f, 1.3f, 1f);

            Renderer r = vfx.GetComponent<Renderer>();
            r.material = new Material(fireShader);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        else
        {
            vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vfx.name = "FirePlaceholder";
            Object.Destroy(vfx.GetComponent<Collider>()); // que no estorbe a la física
            vfx.transform.SetParent(station);
            vfx.transform.localPosition = _fireVfxOffset;
            vfx.transform.localScale = Vector3.one * 0.6f;

            Renderer r = vfx.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.45f, 0.05f);
        }

        // Luz naranja parpadeante en ambos casos: vende el fuego aunque el
        // VFX quede tapado por un mueble.
        Light light = vfx.AddComponent<Light>();
        light.color     = new Color(1f, 0.5f, 0.1f);
        light.range     = 4f;
        light.intensity = 2.5f;
        vfx.AddComponent<FireLightFlicker>();

        return vfx;
    }

    [ContextMenu("Forzar incendio ahora")]
    private void DebugForceFire() => IgniteRandomStation();
}

/// <summary>
/// Parpadeo orgánico para la luz del fuego (ruido Perlin, sin saltos bruscos).
/// Lo añade FireEventManager al placeholder; si el VFX definitivo trae su
/// propia luz, puede reutilizarse añadiéndolo al prefab.
/// </summary>
public class FireLightFlicker : MonoBehaviour
{
    [SerializeField] private float _baseIntensity = 2.5f;
    [SerializeField] private float _flickerAmount = 1.0f;
    [SerializeField] private float _flickerSpeed  = 6f;

    private Light _light;
    private float _seed;

    void Awake()
    {
        _light = GetComponent<Light>();
        _seed  = Random.Range(0f, 100f);
        if (_light != null) _baseIntensity = _light.intensity;
    }

    void Update()
    {
        if (_light == null) return;
        float n = Mathf.PerlinNoise(_seed, Time.time * _flickerSpeed); // 0-1 suave
        _light.intensity = _baseIntensity + (n - 0.5f) * 2f * _flickerAmount;
    }
}
