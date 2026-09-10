using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Ciclo día/noche barato: un ÚNICO directional light (el sol) más luz ambiente,
/// niebla y un volume de color grading. Todo se evalúa con curvas/gradientes, así
/// que cada frame cuesta unos pocos floats y CERO allocations.
///
/// Reglas de rendimiento que respeta (y que hay que mantener si se toca esto):
///  - Solo el sol proyecta sombras en tiempo real. La luz interior "de noche"
///    (lamparas) debe ser Realtime/Mixed y SIN sombras, o se dispara el coste.
///  - El sol se DESACTIVA por completo cuando su intensidad es ~0 (de noche):
///    URP se salta el main light + su shadow pass entero.
///  - No se toca ningún lightmap, ni se instancian materiales, ni se usan
///    <c>Camera.main</c> o <c>FindObjects</c> dentro de Update.
///  - Los valores globales (ambiente, niebla, grading) solo se escriben si
///    cambian de verdad (epsilon), para no re-subir sus constantes cada frame.
/// </summary>
[DisallowMultipleComponent]
public class DayNightCycle : MonoBehaviour
{
    public enum TimeSource
    {
        /// <summary>El ciclo sigue el timer del día de juego (mañana → atardecer).</summary>
        DayManager,
        /// <summary>Reloj propio en bucle, para escenas sin DayManager.</summary>
        Independent
    }

    private const float Epsilon = 1f / 512f;

    [Header("Origen del tiempo")]
    [Tooltip("DayManager: el ciclo va atado al día de juego. Independent: reloj propio en bucle.")]
    [SerializeField] private TimeSource _timeSource = TimeSource.DayManager;
    [Tooltip("Duración del ciclo cuando no hay DayManager (segundos).")]
    [SerializeField] private float _independentCycleSeconds = 120f;
    [Tooltip("Desfase extra del ciclo (se aplica DESPUÉS de recortar la ventana del día).")]
    [Range(0f, 1f)][SerializeField] private float _timeOfDayOffset = 0f;
    [Tooltip("Usar tiempo sin escalar: el ciclo sigue aunque el juego esté pausado (timeScale 0).")]
    [SerializeField] private bool _useUnscaledTime = false;

    [Header("Ventana del día de juego")]
    [Tooltip("Punto del ciclo de cielo en el que EMPIEZA el día de juego " +
             "(0 = sol en el horizonte, 0.5 = mediodía). 0.1 ≈ media mañana.")]
    [Range(0f, 1f)][SerializeField] private float _cycleStart = 0.10f;
    [Tooltip("Punto del ciclo de cielo en el que TERMINA el día de juego. " +
             "1.0 = sol ya puesto (noche); 0.85 ≈ atardecer dorado con el sol todavía arriba.")]
    [Range(0f, 1f)][SerializeField] private float _cycleEnd = 0.85f;

    [Header("Sol")]
    [Tooltip("El único directional light que proyecta sombras. Si se deja vacío se busca uno.")]
    [SerializeField] private Light _sun;
    [Tooltip("Altura máxima del sol (grados) a mitad del día.")]
    [SerializeField] private float _noonElevation = 70f;
    [SerializeField] private float _sunYawStart = -30f;
    [SerializeField] private float _sunYawEnd = 120f;
    [SerializeField] private Gradient _sunColor = DefaultSunColor();
    [Tooltip("Intensidad del sol en función del progreso (0 = amanecer, 1 = noche).")]
    [SerializeField] private AnimationCurve _sunIntensity = DefaultSunIntensity();

    [Header("Luz ambiente (Gradient)")]
    [Tooltip("Cambia el modo de ambiente a Gradient para poder animarlo.")]
    [SerializeField] private bool _driveAmbient = true;
    [SerializeField] private Gradient _ambientSky = DefaultAmbientSky();
    [SerializeField] private Gradient _ambientEquator = DefaultAmbientEquator();
    [SerializeField] private Gradient _ambientGround = DefaultAmbientGround();
    [SerializeField] private AnimationCurve _ambientIntensity = DefaultAmbientIntensity();

    [Header("Niebla")]
    [SerializeField] private bool _driveFog = false;
    [SerializeField] private Gradient _fogColor = DefaultFogColor();
    [SerializeField] private AnimationCurve _fogDensity = DefaultFogDensity();

    [Header("Luces de noche (lamparas interiores)")]
    [Tooltip("Deja vacío y se recogen las luces NON-directional hijas de este objeto. " +
             "Deben ser Realtime o Mixed y sin sombras.")]
    [SerializeField] private Light[] _nightLights;
    [Tooltip("Multiplicador de intensidad de las luces de noche (0 = apagadas).")]
    [SerializeField] private AnimationCurve _nightLightsCurve = DefaultNightLights();

    [Header("Color grading (volume en runtime)")]
    [SerializeField] private bool _enableColorGrading = true;
    [Tooltip("Exposición en EV. Negativo = más oscuro (noche).")]
    [SerializeField] private AnimationCurve _postExposure = DefaultPostExposure();
    [SerializeField] private AnimationCurve _saturation = DefaultSaturation();
    [SerializeField] private Gradient _colorFilter = DefaultColorFilter();

    [Header("Rendimiento")]
    [Tooltip("0 = cada frame. >0 limita cuántas veces por segundo se recalcula la luz.")]
    [SerializeField] private float _updatesPerSecond = 0f;

    private float _independentTime;
    private float[] _nightLightBaseIntensity;
    private float _nextUpdateTime;
    private bool _dayEverStarted;

    // Último estado aplicado: solo escribimos globals si algo cambió (epsilon).
    private float _lastSunIntensity = -1f;
    private Color _lastSunColor = new Color(-1f, -1f, -1f, -1f);
    private Color _lastAmbientSky, _lastAmbientEquator, _lastAmbientGround;
    private float _lastAmbientIntensity = -1f;
    private Color _lastFogColor;
    private float _lastFogDensity = -1f;
    private float _lastNightLights = -1f;

    private GameObject _gradingObject;
    private ColorAdjustments _colorAdjustments;

    private float DeltaTime => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    // ------------------------------------------------------------------ ciclo de vida

    void Awake()
    {
        EnsureDefaults();

        if (_sun == null) _sun = GetComponent<Light>();
        if (_sun == null) _sun = FindSun();
        if (_sun == null)
            Debug.LogWarning("[DayNightCycle] No hay directional light: el ciclo no hará nada.", this);

        CollectNightLights();
        SetupColorGrading();

        // Aplicar el estado de arranque ya, para no mostrar un frame mal iluminado.
        Apply(CurrentProgress(), force: true);
    }

    void OnEnable()
    {
        // El estado de los globals pudo cambiar mientras el componente estaba off.
        Apply(CurrentProgress(), force: true);
    }

    void OnDestroy()
    {
        if (_gradingObject != null) Destroy(_gradingObject);
    }

    void Update()
    {
        if (_updatesPerSecond > 0f)
        {
            if (Time.unscaledTime < _nextUpdateTime) return;
            _nextUpdateTime = Time.unscaledTime + 1f / _updatesPerSecond;
        }

        Apply(CurrentProgress(), force: false);
    }

    // ------------------------------------------------------------------ tiempo

    private float CurrentProgress()
    {
        float t;

        if (_timeSource == TimeSource.DayManager && DayManager.Instance != null)
        {
            // Antes de que arranque el primer día, DayManager.DayProgress vale 1
            // (timeRemaining=0 → progress=1), lo que encendería la luz de NOCHE
            // durante el pequeño delay de arranque. Mientras el día no haya
            // empezado nunca, nos quedamos en mañana.
            bool active = DayManager.Instance.IsDayActive;
            if (!active && !_dayEverStarted)
                t = 0f;
            else
            {
                _dayEverStarted |= active;
                t = Mathf.Clamp01(DayManager.Instance.DayProgress);
            }
        }
        else if (_independentCycleSeconds > 0f)
        {
            _independentTime += DeltaTime;
            _independentTime %= _independentCycleSeconds;
            t = _independentTime / _independentCycleSeconds;
        }
        else
        {
            t = 0f;
        }

        // El día de juego no recorre el ciclo de cielo entero: solo su ventana
        // [_cycleStart, _cycleEnd]. Así arranca de mañana y acaba en atardecer
        // dorado, sin llegar a la noche (que sigue existiendo en las curvas por
        // si algún día se quiere cerrar de noche).
        t = Mathf.Lerp(_cycleStart, _cycleEnd > _cycleStart ? _cycleEnd : 1f, t);

        // Sin desfase no se hace wrap: el fin de día debe quedarse en su valor
        // final, no volver al principio. Solo se da la vuelta al ciclo si el
        // diseñador desplaza el reloj con un offset.
        if (_timeOfDayOffset == 0f)
            return Mathf.Clamp01(t);

        t += _timeOfDayOffset;
        return t - Mathf.Floor(t); // wrap a [0,1)
    }

    // ------------------------------------------------------------------ aplicación

    private void Apply(float t, bool force)
    {
        ApplySun(t, force);
        ApplyAmbient(t, force);
        ApplyFog(t, force);
        ApplyNightLights(t);
        ApplyColorGrading(t);
    }

    private void ApplySun(float t, bool force)
    {
        if (_sun == null) return;

        float intensity = _sunIntensity.Evaluate(t);

        // Desactivar el sol de noche ahorra el main light y su shadow pass enteros.
        bool shouldBeOn = intensity > 1e-4f;
        if (_sun.enabled != shouldBeOn) _sun.enabled = shouldBeOn;
        if (!shouldBeOn) return;

        float elevation = _noonElevation * Mathf.Sin(Mathf.PI * t);
        float yaw = Mathf.Lerp(_sunYawStart, _sunYawEnd, t);
        _sun.transform.rotation = Quaternion.Euler(elevation, yaw, 0f);

        Color color = _sunColor.Evaluate(t);

        if (force || Mathf.Abs(intensity - _lastSunIntensity) > Epsilon)
        {
            _sun.intensity = intensity;
            _lastSunIntensity = intensity;
        }

        if (force || !Approximately(color, _lastSunColor))
        {
            _sun.color = color;
            _lastSunColor = color;
        }
    }

    private void ApplyAmbient(float t, bool force)
    {
        if (!_driveAmbient) return;

        Color sky = _ambientSky.Evaluate(t);
        Color equator = _ambientEquator.Evaluate(t);
        Color ground = _ambientGround.Evaluate(t);
        float intensity = _ambientIntensity.Evaluate(t);

        // El modo Gradiente es lo que permite animar el ambiente sin recalcular nada.
        if (RenderSettings.ambientMode != AmbientMode.Trilight)
            RenderSettings.ambientMode = AmbientMode.Trilight;

        if (force || !Approximately(sky, _lastAmbientSky))
        {
            RenderSettings.ambientSkyColor = sky;
            _lastAmbientSky = sky;
        }
        if (force || !Approximately(equator, _lastAmbientEquator))
        {
            RenderSettings.ambientEquatorColor = equator;
            _lastAmbientEquator = equator;
        }
        if (force || !Approximately(ground, _lastAmbientGround))
        {
            RenderSettings.ambientGroundColor = ground;
            _lastAmbientGround = ground;
        }
        if (force || Mathf.Abs(intensity - _lastAmbientIntensity) > Epsilon)
        {
            RenderSettings.ambientIntensity = intensity;
            _lastAmbientIntensity = intensity;
        }
    }

    private void ApplyFog(float t, bool force)
    {
        if (!_driveFog) return;

        Color color = _fogColor.Evaluate(t);
        float density = _fogDensity.Evaluate(t);

        if (force || !Approximately(color, _lastFogColor))
        {
            RenderSettings.fogColor = color;
            _lastFogColor = color;
        }
        if (force || Mathf.Abs(density - _lastFogDensity) > Epsilon)
        {
            RenderSettings.fogDensity = density;
            _lastFogDensity = density;
        }
    }

    private void ApplyNightLights(float t)
    {
        if (_nightLights == null || _nightLights.Length == 0) return;

        float value = _nightLightsCurve.Evaluate(t);
        if (Mathf.Abs(value - _lastNightLights) <= Epsilon) return;
        _lastNightLights = value;

        bool on = value > 1e-4f;
        for (int i = 0; i < _nightLights.Length; i++)
        {
            Light light = _nightLights[i];
            if (light == null) continue;

            // Componente apagado de verdad: no cuesta en el additional-lights pass.
            if (light.enabled != on) light.enabled = on;
            if (on) light.intensity = _nightLightBaseIntensity[i] * value;
        }
    }

    private void ApplyColorGrading(float t)
    {
        if (_colorAdjustments == null) return;

        _colorAdjustments.postExposure.value = _postExposure.Evaluate(t);
        _colorAdjustments.saturation.value = _saturation.Evaluate(t);
        _colorAdjustments.colorFilter.value = _colorFilter.Evaluate(t);
    }

    // ------------------------------------------------------------------ setup

    private void CollectNightLights()
    {
        if (_nightLights == null || _nightLights.Length == 0)
        {
            var found = new System.Collections.Generic.List<Light>();
            foreach (var light in GetComponentsInChildren<Light>(true))
            {
                if (light == _sun || light.type == LightType.Directional) continue;
                found.Add(light);
            }
            _nightLights = found.ToArray();
        }

        _nightLightBaseIntensity = new float[_nightLights.Length];
        for (int i = 0; i < _nightLights.Length; i++)
        {
            Light light = _nightLights[i];
            _nightLightBaseIntensity[i] = light != null ? light.intensity : 0f;

#if UNITY_EDITOR
            // lightmapBakeType solo existe en el Editor; en el player la luz simplemente
            // no responde si venía Baked, así que avisamos al autor aquí.
            if (light != null && light.lightmapBakeType == LightmapBakeType.Baked)
                Debug.LogWarning($"[DayNightCycle] '{light.name}' está en Baked: su intensidad no " +
                                 "puede animarse. Ponla en Realtime o Mixed (sin sombras).", light);
#endif
        }
    }

    private void SetupColorGrading()
    {
        if (!_enableColorGrading) return;

        // Perfil creado en runtime (no se toca ningún asset ni se ensucia en el editor).
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _colorAdjustments = profile.Add<ColorAdjustments>(true);

        _gradingObject = new GameObject("DayNightColorGrading");
        _gradingObject.transform.SetParent(transform, false);

        var volume = _gradingObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1000f; // por encima del Global Volume de la escena
        volume.sharedProfile = profile;
    }

    private static Light FindSun()
    {
        var lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        foreach (var light in lights)
            if (light.type == LightType.Directional) return light;
        return null;
    }

    private static bool Approximately(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= Epsilon
            && Mathf.Abs(a.g - b.g) <= Epsilon
            && Mathf.Abs(a.b - b.b) <= Epsilon
            && Mathf.Abs(a.a - b.a) <= Epsilon;
    }

    private void EnsureDefaults()
    {
        // Self-healing: si el componente se creó por código (sin serializar) los
        // gradientes/curvas pueden venir vacíos y evaluarían a negro.
        if (_sunColor == null || _sunColor.colorKeys.Length == 0) _sunColor = DefaultSunColor();
        if (_sunIntensity == null || _sunIntensity.length == 0) _sunIntensity = DefaultSunIntensity();
        if (_ambientSky == null || _ambientSky.colorKeys.Length == 0) _ambientSky = DefaultAmbientSky();
        if (_ambientEquator == null || _ambientEquator.colorKeys.Length == 0) _ambientEquator = DefaultAmbientEquator();
        if (_ambientGround == null || _ambientGround.colorKeys.Length == 0) _ambientGround = DefaultAmbientGround();
        if (_ambientIntensity == null || _ambientIntensity.length == 0) _ambientIntensity = DefaultAmbientIntensity();
        if (_fogColor == null || _fogColor.colorKeys.Length == 0) _fogColor = DefaultFogColor();
        if (_fogDensity == null || _fogDensity.length == 0) _fogDensity = DefaultFogDensity();
        if (_nightLightsCurve == null || _nightLightsCurve.length == 0) _nightLightsCurve = DefaultNightLights();
        if (_postExposure == null || _postExposure.length == 0) _postExposure = DefaultPostExposure();
        if (_saturation == null || _saturation.length == 0) _saturation = DefaultSaturation();
        if (_colorFilter == null || _colorFilter.colorKeys.Length == 0) _colorFilter = DefaultColorFilter();
    }

    // ------------------------------------------------------------------ valores por defecto

    private static AnimationCurve Smooth(params Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);
        return curve;
    }

    private static Gradient Gradient2(Color start, Color end)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }

    private static Gradient GradientN(params (float t, Color c)[] stops)
    {
        var colorKeys = new GradientColorKey[stops.Length];
        var alphaKeys = new GradientAlphaKey[stops.Length];
        for (int i = 0; i < stops.Length; i++)
        {
            colorKeys[i] = new GradientColorKey(stops[i].c, stops[i].t);
            alphaKeys[i] = new GradientAlphaKey(1f, stops[i].t);
        }
        var gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private static AnimationCurve DefaultSunIntensity() => Smooth(
        new Keyframe(0.00f, 0.5f),
        new Keyframe(0.10f, 1.3f),
        new Keyframe(0.40f, 2.6f),
        new Keyframe(0.55f, 2.8f),
        new Keyframe(0.78f, 1.3f),
        new Keyframe(0.90f, 0.3f),
        new Keyframe(1.00f, 0.0f));

    private static Gradient DefaultSunColor() => GradientN(
        (0.00f, new Color(1.00f, 0.55f, 0.30f)),  // amanecer
        (0.15f, new Color(1.00f, 0.85f, 0.70f)),
        (0.45f, new Color(1.00f, 0.97f, 0.92f)),  // mediodía
        (0.75f, new Color(1.00f, 0.80f, 0.55f)),
        (0.92f, new Color(1.00f, 0.48f, 0.26f)),  // atardecer
        (1.00f, new Color(0.30f, 0.36f, 0.60f))); // noche

    private static Gradient DefaultAmbientSky() => GradientN(
        (0.00f, new Color(0.26f, 0.27f, 0.30f)),
        (0.50f, new Color(0.52f, 0.58f, 0.66f)),
        (1.00f, new Color(0.08f, 0.10f, 0.18f)));

    private static Gradient DefaultAmbientEquator() => GradientN(
        (0.00f, new Color(0.16f, 0.17f, 0.19f)),
        (0.50f, new Color(0.33f, 0.36f, 0.40f)),
        (1.00f, new Color(0.05f, 0.06f, 0.11f)));

    private static Gradient DefaultAmbientGround() => GradientN(
        (0.00f, new Color(0.07f, 0.07f, 0.06f)),
        (0.50f, new Color(0.14f, 0.13f, 0.11f)),
        (1.00f, new Color(0.02f, 0.02f, 0.04f)));

    private static AnimationCurve DefaultAmbientIntensity() => Smooth(
        new Keyframe(0.00f, 0.55f),
        new Keyframe(0.35f, 1.00f),
        new Keyframe(0.70f, 0.85f),
        new Keyframe(1.00f, 0.30f));

    private static Gradient DefaultFogColor() => GradientN(
        (0.00f, new Color(0.55f, 0.55f, 0.55f)),
        (0.50f, new Color(0.70f, 0.74f, 0.80f)),
        (1.00f, new Color(0.06f, 0.07f, 0.12f)));

    private static AnimationCurve DefaultFogDensity() => Smooth(
        new Keyframe(0.00f, 0.010f),
        new Keyframe(0.50f, 0.006f),
        new Keyframe(1.00f, 0.018f));

    private static AnimationCurve DefaultNightLights() => Smooth(
        new Keyframe(0.00f, 0.0f),
        new Keyframe(0.70f, 0.0f),
        new Keyframe(0.85f, 1.0f),
        new Keyframe(1.00f, 1.0f));

    private static AnimationCurve DefaultPostExposure() => Smooth(
        new Keyframe(0.00f, 0.0f),
        new Keyframe(0.50f, 0.0f),
        new Keyframe(0.80f, -0.30f),
        new Keyframe(1.00f, -0.80f));

    private static AnimationCurve DefaultSaturation() => Smooth(
        new Keyframe(0.00f, 1.00f),
        new Keyframe(0.50f, 1.05f),
        new Keyframe(1.00f, 0.80f));

    private static Gradient DefaultColorFilter() => GradientN(
        (0.00f, Color.white),
        (0.60f, Color.white),
        (1.00f, new Color(0.60f, 0.68f, 1.00f)));
}
