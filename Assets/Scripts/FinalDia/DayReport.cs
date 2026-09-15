using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;


public class DayReport : MonoBehaviour
{
    public static DayReport Instance { get; private set; }

    // Contadores del día en curso
    public int DishesServed     { get; private set; }
    public int ClientsSatisfied { get; private set; }
    public int ClientsAngry     { get; private set; }
    public int MoneyEarned      { get; private set; }
    public int MoneySpent       { get; private set; }

    public int TotalClients => ClientsSatisfied + ClientsAngry;
    public int NetMoney     => MoneyEarned - MoneySpent;

    [Header("Cinemática de fin de día")]
    [Tooltip("Ancla de la cinemática: pose final de la cámara (posición + rotación). " +
             "Coloca un Transform vacío en la escena apuntando al plano que quieras " +
             "enseñar al cerrar el día. Vacía = subida procedural (sube y pica abajo " +
             "desde donde esté la cámara).")]
    [SerializeField] private Transform _endOfDayCameraAnchor;
    [Tooltip("Segundos de la subida de cámara al ángulo alto.")]
    [SerializeField, Min(0.1f)] private float _cameraRiseSeconds = 2.5f;
    [Tooltip("Metros que gana la cámara en altura durante la subida.")]
    [SerializeField, Min(0.1f)] private float _cameraRiseHeight = 6.5f;
    [Tooltip("Inclinación final hacia abajo, en grados (0 = horizonte, 90 = al suelo).")]
    [SerializeField, Range(5f, 89f)] private float _highAnglePitch = 65f;
    [Tooltip("Segundos del anochecer acelerado (el DayNightCycle va hasta la noche).")]
    [SerializeField, Min(0.1f)] private float _nightRampSeconds = 3.5f;
    [Tooltip("Pausa en plena noche antes de mostrar el panel de resultados.")]
    [SerializeField, Min(0f)] private float _holdBeforePanelSeconds = 0.8f;

    private Camera _cinematicCam;
    private Action _onCinematicDone;
    private Coroutine _cinematicRoutine;
    private Vector3 _endPos;
    private Quaternion _endRot;
    private bool _endPoseReady;
    private bool _subscribedSkip;

    /// <summary>Hay una cinemática de fin de día en curso: el panel de resultados espera.</summary>
    public bool IsCinematicPlaying { get; private set; }

    // Suscripción al DayManager para resetear contadores al empezar el día.
    private bool _subscribedDay = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    private void OnEnable()
    {
        TrySubscribe();
        TrySubscribeSkip();
    }

    private void OnDisable()
    {
        if (_subscribedDay && DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= ResetCounters;
        _subscribedDay = false;

        if (_subscribedSkip && InputManager.Instance != null)
            InputManager.Instance.OnInteractPressed -= OnInteractPressedSkip;
        _subscribedSkip = false;
    }

    private void Start()
    {
        ResetCounters();
    }

    private void Update()
    {
        if (!_subscribedDay)
            TrySubscribe();
        if (!_subscribedSkip)
            TrySubscribeSkip();
    }

    private void TrySubscribe()
    {
        if (!_subscribedDay && DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += ResetCounters;
            _subscribedDay = true;
        }
    }

    // El InputManager puede no existir aún en OnEnable (orden de ejecución);
    // reintentamos desde Update hasta que aparezca (patrón de TrySubscribe).
    private void TrySubscribeSkip()
    {
        if (!_subscribedSkip && InputManager.Instance != null)
        {
            InputManager.Instance.OnInteractPressed += OnInteractPressedSkip;
            _subscribedSkip = true;
        }
    }

    /// <summary>E durante la cinemática: la salta (warp de cámara + panel).</summary>
    private void OnInteractPressedSkip()
    {
        if (IsCinematicPlaying)
            SkipCinematic();
    }

    /// <summary>Pone todos los contadores a cero. Se llama al empezar cada día.</summary>
    public void ResetCounters()
    {
        DishesServed     = 0;
        ClientsSatisfied = 0;
        ClientsAngry     = 0;
        MoneyEarned      = 0;
        MoneySpent       = 0;
    }

    // --- Registro desde Client ---

    /// <summary>Un cliente se fue satisfecho. Cuenta como plato servido.</summary>
    public void RegisterSatisfiedClient()
    {
        ClientsSatisfied++;
        DishesServed++;
    }

    /// <summary>Un cliente se fue enfadado (sin pagar).</summary>
    public void RegisterAngryClient()
    {
        ClientsAngry++;
    }

    // --- Registro de dinero (directo, llamado explícitamente) ---

    /// <summary>Registra dinero ganado en el día (pago de un cliente).</summary>
    public void RegisterEarnings(int amount)
    {
        if (amount > 0) MoneyEarned += amount;
    }

    /// <summary>Registra dinero gastado en el día (compras, etc.).</summary>
    public void RegisterSpending(int amount)
    {
        if (amount > 0) MoneySpent += amount;
    }

    // --- Nota del día ---

    /// <summary>% de clientes satisfechos sobre el total (0-100).</summary>
    public float SatisfactionPercent
    {
        get
        {
            if (TotalClients == 0) return 0f;
            return (ClientsSatisfied / (float)TotalClients) * 100f;
        }
    }

    /// <summary>
    /// Nota del día (F-A). Escala del GDD: A>=90, B>=80, C>=70, D>=60, E>=50, F<50.
    /// Si no vino ningún cliente, devuelve 'F'.
    /// </summary>
    public char GetGrade()
    {
        if (TotalClients == 0) return 'F';
        float pct = SatisfactionPercent;
        if (pct >= 90f) return 'A';
        if (pct >= 80f) return 'B';
        if (pct >= 70f) return 'C';
        if (pct >= 60f) return 'D';
        if (pct >= 50f) return 'E';
        return 'F';
    }

    // --- Cinemática de fin de día ---

    /// <summary>
    /// Secuencia de cierre del día, que StatsPanel intenta lanzar ANTES de
    /// mostrar el panel: mueve la cámara al plano designado (ancla de escena,
    /// o subida al ángulo alto si no hay), acelera el ciclo hasta la noche y
    /// entonces llama a <paramref name="onComplete"/> (que muestra el panel).
    /// Se salta con E (SkipCinematic). Devuelve false si no hay nada que
    /// cinemáticar (ni cámara ni ciclo): el panel debe salir directamente.
    /// </summary>
    public bool TryPlayEndOfDayCinematic(Action onComplete)
    {
        if (IsCinematicPlaying || !isActiveAndEnabled) return false;

        _cinematicCam = Camera.main;
        DayNightCycle cycle = FindAnyObjectByType<DayNightCycle>();

        if (_cinematicCam == null && cycle == null)
            return false;

        _onCinematicDone = onComplete;
        IsCinematicPlaying = true;
        _endPoseReady = false;
        _cinematicRoutine = StartCoroutine(CinematicRoutine(cycle));
        return true;
    }

    private IEnumerator CinematicRoutine(DayNightCycle cycle)
    {
        // Congela al jugador durante el corte (el panel reentrará en pausa y
        // EnterPause es no-op si ya lo está; ExitPause lo suelta al pasar de día).
        if (InputManager.Instance != null)
            InputManager.Instance.EnterPause();

        try
        {
            // 1) Subida al ángulo alto. Con Cinemachine, el brain reescribe la
            //    transform de la cámara cada frame — y su modo SmartUpdate deja
            //    de actualizar la vcam cuando el objetivo de seguimiento está
            //    quieto (el jugador lo está durante el corte), además de
            //    acumular un salto de damping cuando la actualización vuelve.
            //    Apagándolo, la transform de la cámara es nuestra y la animamos
            //    a pelo; al día siguiente la escena recarga y el brain vuelve solo.
            if (_cinematicCam != null)
            {
                var brain = _cinematicCam.GetComponent<CinemachineBrain>();
                if (brain != null) brain.enabled = false;

                // La pose final se decide UNA VEZ aquí: SkipCinematic warpéa
                // exactamente al mismo sitio al que la subida habría llegado.
                ComputeEndPose(out _endPos, out _endRot);
                _endPoseReady = true;
                yield return RiseDirect(_endPos, _endRot);
            }

            // 2) Anochecer acelerado; el ciclo se queda clavado en la noche.
            if (cycle != null)
            {
                cycle.BeginNightRamp(_nightRampSeconds);
                yield return new WaitForSecondsRealtime(_nightRampSeconds);
            }

            // 3) Respiro en plena noche antes de que salga el panel.
            if (_holdBeforePanelSeconds > 0f)
                yield return new WaitForSecondsRealtime(_holdBeforePanelSeconds);
        }
        finally
        {
            IsCinematicPlaying = false;
        }

        Action done = _onCinematicDone;
        _onCinematicDone = null;
        done?.Invoke();
    }

    // Pose final del corte: el ancla colocada en la escena si hay (posición y
    // rotación exactas, el encuadre lo decide el diseñador), o subida
    // procedural (misma XZ, subir y picar abajo) si no. Siempre parte de la
    // pose en la que la cámara esté al empezar el corte.
    private void ComputeEndPose(out Vector3 pos, out Quaternion rot)
    {
        Transform anchor = _endOfDayCameraAnchor;
        if (anchor != null)
        {
            pos = anchor.position;
            rot = anchor.rotation;
        }
        else
        {
            Transform t = _cinematicCam.transform;
            pos = t.position + Vector3.up * _cameraRiseHeight;
            rot = HighAngleRotation(t.rotation);
        }
    }

    /// <summary>Salta la cinemática: warp de la cámara al plano final designado,
    /// el anochecer se completa al instante y sale el panel de resultados.
    /// La dispara una pulsación de E durante el corte (InputManager).</summary>
    public void SkipCinematic()
    {
        if (!IsCinematicPlaying) return;
        IsCinematicPlaying = false; // primero: una segunda E no reentra aquí

        if (_cinematicRoutine != null)
        {
            StopCoroutine(_cinematicRoutine);
            _cinematicRoutine = null;
        }

        // Warp instantáneo al mismo plano al que la subida habría llegado.
        if (_endPoseReady && _cinematicCam != null)
        {
            _cinematicCam.transform.position = _endPos;
            _cinematicCam.transform.rotation = _endRot;
        }

        // El panel se muestra sobre la noche diseñada: completa el anochecer ya
        // (duración ~0; si la subida no había acabado, la noche también salta).
        DayNightCycle cycle = FindAnyObjectByType<DayNightCycle>();
        if (cycle != null)
            cycle.BeginNightRamp(0.01f);

        Action done = _onCinematicDone;
        _onCinematicDone = null;
        done?.Invoke();
    }

    // Subida: interpola de la pose de partida a la pose final que decidió
    // ComputeEndPose (ancla de escena o subida procedural).
    private IEnumerator RiseDirect(Vector3 endPos, Quaternion endRot)
    {
        Transform t = _cinematicCam.transform;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        yield return EaseRoutine(_cameraRiseSeconds, k =>
        {
            t.position = Vector3.Lerp(startPos, endPos, k);
            t.rotation = Quaternion.Slerp(startRot, endRot, k);
        });
    }

    private static IEnumerator EaseRoutine(float seconds, Action<float> apply)
    {
        float time = 0f;
        while (time < seconds)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / seconds));
            apply(k);
            yield return null;
        }
        apply(1f);
    }

    // Rotación final: mismo encuadre horizontal que ahora, picando abajo hasta
    // _highAnglePitch. Se construye desde el forward real (no desde los ángulos
    // de Euler, que con pitch grandes se leen mal).
    private Quaternion HighAngleRotation(Quaternion current)
    {
        Vector3 forward = current * Vector3.forward;
        Vector3 flat = new Vector3(forward.x, 0f, forward.z);
        if (flat.sqrMagnitude < 1e-6f)
        {
            Vector3 right = current * Vector3.right;
            flat = new Vector3(right.x, 0f, right.z);
        }
        if (flat.sqrMagnitude < 1e-6f)
            flat = Vector3.forward;
        flat.Normalize();

        float yaw = Mathf.Atan2(flat.x, flat.z);
        float pitch = _highAnglePitch * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(
            Mathf.Sin(yaw) * Mathf.Cos(pitch),
            -Mathf.Sin(pitch),
            Mathf.Cos(yaw) * Mathf.Cos(pitch));
        return Quaternion.LookRotation(dir, Vector3.up);
    }

    [ContextMenu("DEBUG: Cinemática de fin de día")]
    private void DebugPlayCinematic() => TryPlayEndOfDayCinematic(null);
}