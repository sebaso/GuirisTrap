using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Cliente de playa: en vez de entrar al chiringuito, se queda en su zona de
// playa (una agrupación de tumbonas y sombrillas que elige al nacer) y allí
// se tumba, se sienta en la arena, se pone a la sombra o pasea cerca. Es puro
// ambiente: no paga, no pide comida y no cuenta para el cierre del día.
// El PlayeroSpawner elige su próxima actividad; esta clase la ejecuta.
[RequireComponent(typeof(NavMeshAgent))]
public class Playero : MonoBehaviour
{
    public enum State
    {
        Paseando,       // caminando sin rumbo / esperando la siguiente actividad
        YendoASitio,    // caminando hacia su spot reclamado o su asiento de arena
        TomandoElSol,   // sentado en una tumbona
        ALaSombra,      // de pie bajo la sombrilla
        SentadoEnArena, // sentado en la arena de su zona
        Marchando       // hacia la salida para despawnear
    }

    [Header("Modelo")]
    [Tooltip("Mismos modelos animados que el prefab de Client.")]
    public GameObject[] clientModels;
    [Tooltip("Offset local del ModelPivot al sentarse en la tumbona; se restaura al levantarse.")]
    public Vector3 loungePivotOffset = new(0f, -0.15f, 0f);
    [Tooltip("Segundos levantándose de la tumbona antes de volver a andar.")]
    public float standUpSeconds = 1f;

    [Header("Movimiento")]
    [Tooltip("Velocidad del NavMeshAgent (se sortea al nacer dentro de este rango).")]
    public Vector2 moveSpeedRange = new(2.2f, 3f);
    [Tooltip("Prioridad de evitación del agente (menor = aparta antes).")]
    [Range(0, 99)] public int avoidancePriority = 75;
    [Tooltip("Faltando menos que esto para el destino ya se considera llegado.")]
    public float arrivalTolerance = 0.6f;
    [Tooltip("Margen extra sobre el stoppingDistance del agente para dar por llegada la caminata.")]
    public float arrivalBuffer = 0.15f;
    [Tooltip("Anti-atasco: si un paseo supera estos segundos, se da por terminado.")]
    public float walkTimeout = 30f;

    [Header("Tiempos de actividad")]
    [Tooltip("Pausa entre una actividad y la siguiente (el spawner la sobrescribe con su 'pausa entre paseos').")]
    public Vector2 decisionPauseRange = new(2f, 4f);
    [Tooltip("Cabezazos en la sombra: segundos entre giro y giro.")]
    public Vector2 lookAroundInterval = new(2.5f, 6f);
    [Tooltip("Giro máximo de cada cabezazo, en grados.")]
    public float lookAroundJitter = 70f;
    [Tooltip("Grados por segundo al girar la cabeza esperando a la sombra.")]
    public float lookTurnSpeed = 40f;

    // Valores del parámetro "State" del ClientAnimator (mismos controladores):
    // 3 = Sentarse, 6 = Locomotion (mezclada por "Speed").
    private const int AnimStateSit = 3;
    private const int AnimStateWalk = 6;

    public State CurrentState { get; private set; } = State.Paseando;

    // Temporadores de la actividad en curso (tumbona o sombra): cuánto duraba,
    // cuánto queda y en qué punto va. Valen 0 fuera de actividad.
    public float ActivityDuration { get; private set; }
    public float ActivityRemaining => _activityRemaining;
    public float ActivityProgress => ActivityDuration > 0f
        ? Mathf.Clamp01(1f - _activityRemaining / ActivityDuration)
        : 0f;

    // Segundos lleve en el estado actual (para animaciones, HUD o depuración).
    public float StateElapsed => Time.time - _stateEnteredAt;

    // Todos los playeros vivos, para expulsarlos al acabar el día.
    public static readonly List<Playero> All = new();

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _modelPivot;
    private bool _initialized;

    private PlayeroSpot _spot;
    private float _activityRemaining;  // lo que queda de tumbona/sombra
    private float _stateEnteredAt;
    private float _idleUntil;          // pausa antes del siguiente paseo
    private float _nextLookAt;         // próximos giros de cabeza en la sombra
    private float _lookTargetYaw;
    private bool _standingUp;          // beat de levantarse antes de andar
    private Vector3 _exitPosition;
    private PlayeroSpawner _spawner;

    private Vector3 _anchor;           // zona de playa asignada al nacer
    private bool _anchorSet;
    private Vector3 _sandSeatPosition; // asiento de arena elegido
    private float _sandLookYaw;        // hacia dónde mira sentado (grados)
    private bool _sandSeatedTarget;    // caminando hacia el asiento de arena

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Random.Range(moveSpeedRange.x, moveSpeedRange.y);
        _agent.avoidancePriority = avoidancePriority;
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
        ReleaseSpot();
    }

    void Start()
    {
        Initialize();
    }

    // Misma puesta en escena del modelo que Client.initialize: instancia uno al
    // azar bajo un ModelPivot (el controlador anima esa ruta) y lo baja 0.5
    // para asentar los pies en el suelo.
    public void Initialize()
    {
        if (_initialized) return;

        if (clientModels != null && clientModels.Length > 0)
        {
            GameObject selectedModel = Instantiate(clientModels[Random.Range(0, clientModels.Length)],
                transform.position, Quaternion.identity);
            selectedModel.transform.localRotation = Quaternion.identity;

            _modelPivot = new GameObject("ModelPivot").transform;
            _modelPivot.SetParent(transform, false);
            selectedModel.transform.SetParent(_modelPivot, true);
            selectedModel.transform.position += new Vector3(0f, -0.5f, 0f);

            _animator = selectedModel.GetComponentInChildren<Animator>();
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        _initialized = true;
        SetAnimState(AnimStateWalk);
    }

    // ------------------------------------------------------------------
    // API llamada por PlayeroSpawner
    // ------------------------------------------------------------------
    public void SetSpawner(PlayeroSpawner spawner) => _spawner = spawner;

    public void SetExitPosition(Vector3 exit) => _exitPosition = exit;

    // La zona de playa de este playero: todo lo que haga (sitio, arena, paseo)
    // se resuelve cerca de este punto.
    public void SetAnchor(Vector3 anchor)
    {
        _anchor = anchor;
        _anchorSet = true;
    }

    public Vector3 HomePosition => _anchor;
    public bool HasHome => _anchorSet;

    public void PasearHacia(Vector3 destino)
    {
        if (CurrentState == State.Marchando) return;
        ReleaseSpot();
        _standingUp = false;
        SetState(State.Paseando);
        WalkTo(destino);
    }

    public bool OcuparSpot(PlayeroSpot spot, float duracion)
    {
        if (CurrentState == State.Marchando) return false;
        if (spot == null || !spot.IsFree) return false;

        // si no hay camino hasta el sitio, ni intentar: evita "teletransportes"
        var path = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, spot.transform.position, NavMesh.AllAreas, path);
        if (path.status != NavMeshPathStatus.PathComplete) return false;

        ReleaseSpot();
        _spot = spot;
        spot.Claim(this);
        _activityRemaining = Mathf.Max(0f, duracion);
        ActivityDuration = _activityRemaining;
        _standingUp = false;
        SetState(State.YendoASitio);
        WalkTo(spot.transform.position);
        return true;
    }

    // Se sienta en la arena cerca de su zona (no ocupa ningún PlayeroSpot):
    // camina al punto elegido y se queda ahí el tiempo indicado.
    public bool SentarseEnArena(Vector3 lugar, float mirandoYaw, float duracion)
    {
        if (CurrentState == State.Marchando) return false;

        var path = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, lugar, NavMesh.AllAreas, path);
        if (path.status != NavMeshPathStatus.PathComplete) return false;

        ReleaseSpot();
        _sandSeatPosition = lugar;
        _sandLookYaw = mirandoYaw;
        _sandSeatedTarget = true;
        _activityRemaining = Mathf.Max(0f, duracion);
        ActivityDuration = _activityRemaining;
        _standingUp = false;
        SetState(State.YendoASitio);
        WalkTo(lugar);
        return true;
    }

    public void SendAway()
    {
        if (CurrentState == State.Marchando) return;

        bool seated = CurrentState == State.TomandoElSol || CurrentState == State.SentadoEnArena;
        ReleaseSpot();
        SetState(State.Marchando);
        _stateEnteredAt = Time.time;

        // Si está en la tumbona se levanta primero, como los clientes.
        _standingUp = seated;
        if (seated)
        {
            SetAnimState(AnimStateWalk);
            if (_modelPivot != null) _modelPivot.localPosition = Vector3.zero;
        }
        WalkTo(_exitPosition);
    }

    public void NearestExit()
    {
        if (_spawner != null)
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < _spawner.exitPoint.Length; i++)
            {
                if (_spawner.exitPoint[i] == null) continue;
                var path = new NavMeshPath();
                NavMesh.CalculatePath(transform.position, _spawner.exitPoint[i].position, NavMesh.AllAreas, path);
                if (path.status != NavMeshPathStatus.PathComplete) continue;
                float dist = (transform.position - _spawner.exitPoint[i].position).sqrMagnitude;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = _spawner.exitPoint[i];
                }
            }
            if (nearest != null)
                _exitPosition = nearest.position;
        }
    }

    public static void SendAllAway()
    {
        List<Playero> playeros = new(All);
        foreach (var p in playeros)
            if (p != null)
                p.SendAway();
    }

    // ------------------------------------------------------------------
    private void Update()
    {
        if (!_initialized) Initialize();

        if (_animator != null)
            _animator.SetFloat(ClientAnimationParams.Speed, _agent.velocity.magnitude);

        switch (CurrentState)
        {
            case State.Paseando:
                {
                    // el paseo falló (destino inalcanzable) solo si ya ha pasado
                    // un beat: SetDestination tarda un instante en resolverse
                    bool caminoFracasado = SinCamino() && Time.time - _stateEnteredAt > 1.5f;
                    if (!_standingUp && Time.time >= _idleUntil && (HasReachedDestination() || caminoFracasado))
                        RequestNextActivity();
                    break;
                }

            case State.YendoASitio:
                if (_spot == null && !_sandSeatedTarget) { VolverAPasear(0.5f); break; }
                // sin camino (o atascado): soltar el sitio y reorganizarse
                if (TimedOut() || SinCamino()) { VolverAPasear(2f); break; }
                if (HasReachedDestination())
                    EmpezarActividad();
                break;

            case State.TomandoElSol:
            case State.ALaSombra:
            case State.SentadoEnArena:
                _activityRemaining -= Time.deltaTime;
                if (CurrentState == State.ALaSombra || CurrentState == State.SentadoEnArena)
                    TickLookAround();
                if (_activityRemaining <= 0f)
                    Levantarse(standUpSeconds);
                break;

            case State.Marchando:
                if (SinCamino() || HasReachedDestination() || TimedOut())
                    Destroy(gameObject);
                break;
        }
    }

    private void EmpezarActividad()
    {
        Freeze();

        // verificación al llegar: si el sitio (o su conjunto) ya no es suyo,
        // soltarlo y reorganizarse en vez de sentarse donde hay alguien
        if (_spot != null && _spot.occupant != this)
        {
            VolverAPasear(2f);
            return;
        }

        if (_sandSeatedTarget)
        {
            _sandSeatedTarget = false;
            transform.position = _sandSeatPosition;
            _lookTargetYaw = _sandLookYaw;
            transform.rotation = Quaternion.Euler(0f, _lookTargetYaw, 0f);
            SetAnimState(AnimStateSit);
            SetState(State.SentadoEnArena);
            return;
        }

        if (_spot.kind == PlayeroSpot.Kind.Tumbona)
        {
            // se sienta sobre la tumbona: pose ajustada al spot (altura del
            // asiento y orientación a lo largo de la hamaca). El forward del
            // spot sale del AABB del mesh y es ambiguo en signo: girar 180
            // para que el playero mire hacia el pie de la hamaca, no al respaldo.
            transform.position = _spot.transform.position;
            transform.rotation = _spot.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            if (_modelPivot != null) _modelPivot.localPosition = loungePivotOffset;
            SetAnimState(AnimStateSit);
            SetState(State.TomandoElSol);
        }
        else
        {
            transform.position = _spot.transform.position;
            _lookTargetYaw = _spot.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, _lookTargetYaw, 0f);
            SetAnimState(AnimStateWalk);
            SetState(State.ALaSombra);
        }

        _stateEnteredAt = Time.time;
    }

    private void Levantarse(float beat)
    {
        ReleaseSpot();
        SetAnimState(AnimStateWalk);
        if (_modelPivot != null) _modelPivot.localPosition = Vector3.zero;
        _standingUp = beat > 0f;
        _idleUntil = Time.time + Mathf.Max(0.1f, beat);
        SetState(State.Paseando);
        Freeze();
    }

    private void VolverAPasear(float delay)
    {
        ReleaseSpot();
        _standingUp = false;
        _idleUntil = Time.time + Mathf.Max(0f, delay);
        SetState(State.Paseando);
    }

    private void TickLookAround()
    {
        if (Time.time < _nextLookAt) return;
        _nextLookAt = Time.time + Random.Range(lookAroundInterval.x, lookAroundInterval.y);
        _lookTargetYaw += Random.Range(-lookAroundJitter, lookAroundJitter);
    }

    private void RequestNextActivity()
    {
        if (_spawner != null)
            _spawner.RequestNextActivity(this);
        else
            PasearHacia(SampleCerca(transform.position, 8f));

        // respiro entre decisiones aunque el plan elegido sea inmediato
        _idleUntil = Time.time + Random.Range(decisionPauseRange.x, decisionPauseRange.y);
    }

    // El spawner afina la pausa entre decisiones al spawear (usa su
    // "pausa entre paseos").
    public void SetDecisionPause(Vector2 range) => decisionPauseRange = range;

    private void LateUpdate()
    {
        // giro suave mientras espera en la sombra
        if (CurrentState == State.ALaSombra)
        {
            float current = transform.eulerAngles.y;
            float next = Mathf.MoveTowardsAngle(current, _lookTargetYaw, lookTurnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, next, 0f);
        }
    }

    // ------------------------------------------------------------------
    private void ReleaseSpot()
    {
        if (_spot != null) _spot.Release(this);
        _spot = null;
        ActivityDuration = 0f;
        _sandSeatedTarget = false;
    }

    private void WalkTo(Vector3 destination)
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
            _stateEnteredAt = Time.time;
        }
    }

    private void Freeze()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
        if (_agent != null) _agent.velocity = Vector3.zero;
    }

    private bool HasReachedDestination()
    {
        if (_agent.pathPending) return false;
        if (!_agent.hasPath) return false;
        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid) return false;
        if (_agent.remainingDistance <= _agent.stoppingDistance + arrivalBuffer) return true;
        if (_agent.remainingDistance <= arrivalTolerance) return true;
        return false;
    }

    // sin camino viable hasta el destino (p. ej. destino en otra isla de la malla)
    private bool SinCamino()
    {
        return !_agent.pathPending && (!_agent.hasPath || _agent.pathStatus == NavMeshPathStatus.PathInvalid);
    }

    private bool TimedOut() => Time.time - _stateEnteredAt >= walkTimeout;

    private void SetState(State newState)
    {
        CurrentState = newState;
        _stateEnteredAt = Time.time;
    }

    private void SetAnimState(int value)
    {
        if (_animator != null)
            _animator.SetInteger(ClientAnimationParams.State, value);
    }

    private static Vector3 SampleCerca(Vector3 centro, float radio)
    {
        // la malla también cubre tejados y sombrillas (islas desconectadas a
        // otra altura): descartar los puntos que no estén a ras del suelo
        for (int i = 0; i < 6; i++)
        {
            Vector3 candidato = centro + Random.insideUnitSphere * radio;
            candidato.y = centro.y;
            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, radio, NavMesh.AllAreas)
                && Mathf.Abs(hit.position.y - centro.y) < 1.5f)
                return hit.position;
        }
        return centro;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = CurrentState switch
        {
            State.TomandoElSol => new Color(1f, 0.55f, 0.1f),
            State.ALaSombra => new Color(0.3f, 0.7f, 1f),
            State.SentadoEnArena => new Color(0.85f, 0.7f, 0.35f),
            State.Marchando => Color.red,
            _ => Color.green
        };
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, 0.25f);

        // la zona de playa asignada al nacer (línea punteada visual del destino)
        if (_anchorSet)
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(_anchor + Vector3.up * 0.5f, new Vector3(1f, 0.2f, 1f));
            Gizmos.DrawLine(transform.position + Vector3.up * 1.2f, _anchor + Vector3.up * 0.5f);
        }
    }
}
