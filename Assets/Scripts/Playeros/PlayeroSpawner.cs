using System.Collections.Generic;
using UnityEngine;

// Spawner de playeros: durante el día los va sacando por la orilla y les va
// asignando actividades (tumbona, sombra o paseo). Al acabar el día los manda
// de vuelta a la salida. También descubre las tumbonas y sombrillas de la
// playa y crea un PlayeroSpot por cada una (menú de contexto sobre este
// componente: "Descubrir sitios de playa").
public class PlayeroSpawner : MonoBehaviour
{
    [Header("Prefab y puntos")]
    public GameObject playeroPrefab;
    [Tooltip("Punto de la orilla donde aparecen (que esté sobre la NavMesh de la playa).")]
    public Transform[] spawnPoints;
    [Tooltip("Hacia dónde caminan al marcharse.")]
    public Transform[] exitPoint;

    [Header("Descubrimiento de la playa")]
    [Tooltip("Raíz donde viven las tumbonas y sombrillas (p.ej. Escenario_2).")]
    public Transform beachPropsRoot;
    public string loungePrefix = "Tumbona";
    public string shadePrefix = "Sombrilla";
    [Tooltip("Ajuste vertical del asiento del playero sobre la tumbona.")]
    public float loungeHeightOffset = 0f;
    [Tooltip("Distancia máxima entre una sombrilla y una tumbona para contarlas como un conjunto: si una pieza se ocupa, todo el conjunto queda ocupado.")]
    public float setLinkRadius = 4f;
    [Tooltip("Si al arrancar no hay sitios, los descubre automáticamente.")]
    public bool autoDiscover = true;

    [Header("Aforo y ritmo")]
    public int maxPlayeros = 8;
    public float spawnInterval = 14f;
    public Vector2Int burstSize = new(1, 2);
    public float firstSpawnDelay = 3f;

    [Header("Actividades")]
    [Tooltip("Peso de tumbarse en tumbona; el sobrante de la suma es paseo.")]
    [Range(0f, 1f)] public float loungeWeight = 0.5f;
    [Tooltip("Peso de quedarse a la sombra; el sobrante de la suma es paseo.")]
    [Range(0f, 1f)] public float shadeWeight = 0.2f;
    [Tooltip("Peso de sentarse en la arena; el sobrante de la suma es paseo.")]
    [Range(0f, 1f)] public float sandSitWeight = 0.15f;

    [Range(0f, 1f)] public float leaveWeight = 0.05f;

    public Vector2 loungeDuration = new(20f, 45f);
    public Vector2 shadeDuration = new(8f, 20f);
    [Tooltip("Cuánto se quedan sentados en la arena (sin tumbona).")]
    public Vector2 sandSitDuration = new(15f, 45f);
    [Tooltip("Pausa entre paseos; también marca el respiro entre actividades de cada playero.")]
    public Vector2 strollIdle = new(1.5f, 5f);
    [Tooltip("Radio del paseo alrededor de la zona de cada playero.")]
    public float strollRadius = 7f;
    [Tooltip("Segundos que tarda cada playero en levantarse de la tumbona; se aplica al spawear.")]
    public float standUpSeconds = 1f;

    [Header("Zona de cada playero")]
    [Tooltip("Cada playero elige una zona al nacer; un sitio libre cuenta como 'de su zona' si está dentro de este radio.")]
    public float zoneRadius = 12f;
    [Tooltip("Radio del anillo de arena alrededor de la zona donde se sientan.")]
    public float sandSitRadius = 5f;

    private readonly List<Playero> _active = new();
    private readonly List<PlayeroSpot> _spots = new();
    private readonly List<Vector3> _anchors = new(); // centro de cada zona de playa
    private float _timer;

    public int ActiveCount => _active.Count;
    public int SpotCount => _spots.Count;
    public int FreeSpotCount
    {
        get
        {
            int free = 0;
            foreach (var s in _spots)
                if (s != null && s.IsFree) free++;
            return free;
        }
    }
    public int LoungingCount
    {
        get
        {
            int n = 0;
            foreach (var p in _active)
                if (p != null && (p.CurrentState == Playero.State.TomandoElSol
                                  || p.CurrentState == Playero.State.ALaSombra
                                  || p.CurrentState == Playero.State.SentadoEnArena))
                    n++;
            return n;
        }
    }

    void Start()
    {
        _timer = firstSpawnDelay;

        if (_spots.Count == 0 && autoDiscover)
            DiscoverSpots();
    }

    void Update()
    {
        // sin día activo (noche, wind-down) no se spawnea y los que queden se van
        bool dayOver = DayManager.Instance == null
                        || !DayManager.Instance.IsDayActive
                        || DayManager.Instance.IsWindingDown;
        if (dayOver)
        {
            if (_active.Count > 0) SendAllAway();
            return;
        }

        _active.RemoveAll(p => p == null);

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnBurst();
        }
    }

    public void SendAllAway()
    {
        List<Playero> vivos = new(_active);
        foreach (var p in vivos)
            if (p != null)
                p.SendAway();
        _active.Clear();
    }

    // El playero se quedó sin plan: se le asigna la siguiente actividad,
    // siempre cerca de su zona (prefiere los sitios de su zona antes que
    // cruzar toda la playa).
    public void RequestNextActivity(Playero p)
    {
        float roll = Random.value;

        if (roll < loungeWeight && TryGetFreeSpot(p, PlayeroSpot.Kind.Tumbona, out PlayeroSpot lounge))
        {
            p.OcuparSpot(lounge, Random.Range(loungeDuration.x, loungeDuration.y));
            return;
        }

        if (roll < loungeWeight + shadeWeight && TryGetFreeSpot(p, PlayeroSpot.Kind.Sombra, out PlayeroSpot shade))
        {
            p.OcuparSpot(shade, Random.Range(shadeDuration.x, shadeDuration.y));
            return;
        }

        if (roll < loungeWeight + shadeWeight + sandSitWeight
            && TryFindSandSeat(p, out Vector3 seat, out float lookYaw))
        {
            p.SentarseEnArena(seat, lookYaw, Random.Range(sandSitDuration.x, sandSitDuration.y));
            return;
        }

        if (roll < loungeWeight + shadeWeight + sandSitWeight + leaveWeight)
        {
            p.SendAway();
            return;
        }

        p.PasearHacia(RandomStrollTarget(p));
    }

    // ------------------------------------------------------------------
    // Spawning
    // ------------------------------------------------------------------
    private void SpawnBurst()
    {
        if (playeroPrefab == null || spawnPoints == null || exitPoint == null)
        {
            Debug.LogWarning("[PlayeroSpawner] Falta playeroPrefab o spawnPoint.");
            return;
        }

        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        Vector3 basePos = SampleCerca(spawnPos, 2f);
        if (Mathf.Abs(basePos.y - spawnPos.y) > 1.5f)
        {
            Debug.LogWarning("[PlayeroSpawner] El spawnPoint no está sobre la NavMesh; no se spawnea.");
            return;
        }

        int count = Mathf.Min(Random.Range(burstSize.x, burstSize.y + 1), maxPlayeros - _active.Count);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = SampleCerca(basePos, 1.5f);

            GameObject obj = Instantiate(playeroPrefab, pos, Quaternion.identity, transform);
            obj.name = "Playero";
            Playero p = obj.GetComponent<Playero>();
            p.SetSpawner(this);
            p.SetExitPosition(exitPoint[Random.Range(0, exitPoint.Length)].position);
            p.standUpSeconds = standUpSeconds;
            p.SetDecisionPause(strollIdle);
            p.SetAnchor(PickAnchor());
            _active.Add(p);

            // el primer tramo es un paseito hacia su zona para entrar en escena
            p.PasearHacia(RandomStrollTarget(p));
        }
    }

    // Cada playero elige al nacer una zona de playa como su "casa": todo lo
    // que haga después (sitios, arena, paseos) ocurrirá cerca de este punto.
    private Vector3 PickAnchor()
    {
        if (_anchors.Count > 0)
            return _anchors[Random.Range(0, _anchors.Count)];
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        return transform.position;
    }

    // Sitio libre del tipo pedido, prefiriendo los que están dentro de la zona
    // del playero; si su zona está llena, cae a cualquier sitio de la playa.
    private bool TryGetFreeSpot(Playero p, PlayeroSpot.Kind kind, out PlayeroSpot free)
    {
        free = null;
        int count = _spots.Count;
        if (count == 0) return false;

        if (p.HasHome)
        {
            float r2 = zoneRadius * zoneRadius;
            for (int pass = 0; pass < 2; pass++)
            {
                int start = Random.Range(0, count);
                for (int i = 0; i < count; i++)
                {
                    PlayeroSpot s = _spots[(start + i) % count];
                    if (s == null || s.kind != kind || !s.IsFree) continue;
                    bool cerca = (s.transform.position - p.HomePosition).sqrMagnitude <= r2;
                    if (pass == 0 && !cerca) continue;
                    if (pass == 1 && cerca) continue;
                    free = s;
                    return true;
                }
            }
            return false;
        }

        return TryGetFreeSpotAny(kind, out free);
    }

    private bool TryGetFreeSpotAny(PlayeroSpot.Kind kind, out PlayeroSpot free)
    {
        free = null;
        int count = _spots.Count;
        if (count == 0) return false;
        int start = Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            PlayeroSpot s = _spots[(start + i) % count];
            if (s != null && s.kind == kind && s.IsFree)
            {
                free = s;
                return true;
            }
        }
        return false;
    }

    // Un hueco de arena libre alrededor de la zona del playero para sentarse
    // sin tumbona: un anillo a ras de suelo, mirando hacia fuera de la zona
    // (hacia el mar). Devuelve false si el playero no tiene zona.
    private bool TryFindSandSeat(Playero p, out Vector3 seat, out float lookYaw)
    {
        seat = default;
        lookYaw = 0f;
        if (!p.HasHome || sandSitRadius <= 0f) return false;

        Vector3 home = p.HomePosition;
        Vector3 mirandoFuera = Vector3.zero;

        // probar varios sitios del anillo hasta dar con uno con camino
        for (int i = 0; i < 8; i++)
        {
            Vector2 ring = Random.insideUnitCircle.normalized * Mathf.Lerp(sandSitRadius * 0.5f, sandSitRadius, Random.value);
            Vector3 candidato = SampleCerca(home + new Vector3(ring.x, 0f, ring.y), 2.5f);

            var path = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.CalculatePath(p.transform.position, candidato, UnityEngine.AI.NavMesh.AllAreas, path);
            if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete) continue;

            mirandoFuera = candidato - home;
            mirandoFuera.y = 0f;
            if (mirandoFuera.sqrMagnitude < 0.001f) continue;
            seat = candidato;
            lookYaw = Quaternion.LookRotation(mirandoFuera.normalized, Vector3.up).eulerAngles.y;
            return true;
        }
        return false;
    }

    // Paseo alrededor de la zona del playero (no por toda la playa).
    private Vector3 RandomStrollTarget(Playero p)
    {
        Vector3 basePos = p.HasHome ? p.HomePosition : p.transform.position;
        return SampleCerca(basePos + Random.insideUnitSphere * strollRadius, 4f);
    }

    private static Vector3 SampleCerca(Vector3 centro, float radio)
    {
        // descartar tejados/sombrillas: solo puntos a ras del suelo
        for (int i = 0; i < 6; i++)
        {
            Vector3 candidato = centro + Random.insideUnitSphere * radio;
            candidato.y = centro.y;
            if (UnityEngine.AI.NavMesh.SamplePosition(candidato, out UnityEngine.AI.NavMeshHit hit, radio, UnityEngine.AI.NavMesh.AllAreas)
                && Mathf.Abs(hit.position.y - centro.y) < 1.5f)
                return hit.position;
        }
        return centro;
    }

    // ------------------------------------------------------------------
    // Descubrimiento de tumbonas y sombrillas
    // ------------------------------------------------------------------
    [ContextMenu("Descubrir sitios de playa")]
    public void DiscoverSpots()
    {
        if (beachPropsRoot == null)
        {
            Debug.LogWarning("[PlayeroSpawner] beachPropsRoot no asignado; no se descubre nada.");
            return;
        }

        // rellenar desde cero: los sitios ya creados en el editor se conservan,
        // los props sin sitio reciben uno nuevo
        _spots.Clear();
        _anchors.Clear();

        var props = new List<(Transform t, PlayeroSpot.Kind kind)>();
        foreach (Transform t in beachPropsRoot.GetComponentsInChildren<Transform>(false))
        {
            if (StartsWith(t.name, loungePrefix)) props.Add((t, PlayeroSpot.Kind.Tumbona));
            else if (StartsWith(t.name, shadePrefix)) props.Add((t, PlayeroSpot.Kind.Sombra));
        }

        if (props.Count == 0)
        {
            Debug.LogWarning($"[PlayeroSpawner] No se encontró ningún '{loungePrefix}*' ni '{shadePrefix}*' bajo {beachPropsRoot.name}.");
            return;
        }

        // agrupar props por proximidad para conocer cada "zona" de playa
        var clusters = new List<List<(Transform t, PlayeroSpot.Kind kind)>>();
        foreach (var prop in props)
        {
            List<(Transform, PlayeroSpot.Kind)> mine = null;
            foreach (var c in clusters)
            {
                if (Vector3.Distance(c[0].t.position, prop.t.position) < 16f) { mine = c; break; }
            }
            if (mine == null) { mine = new List<(Transform, PlayeroSpot.Kind)>(); clusters.Add(mine); }
            mine.Add(prop);
        }

        int nuevos = 0;
        foreach (var cluster in clusters)
        {
            Vector3 centro = Vector3.zero;
            foreach (var p in cluster) centro += p.t.position;
            centro /= cluster.Count;
            _anchors.Add(centro);

            foreach (var (t, kind) in cluster)
            {
                // ¿ya cuelga un sitio de este prop (creado en una sesión anterior)?
                PlayeroSpot existente = t.GetComponentInChildren<PlayeroSpot>(true);
                if (existente == null)
                {
                    existente = CreateSpot(t, kind, centro);
                    nuevos++;
                }
                _spots.Add(existente);
            }
        }

        // unir cada sombrilla con las tumbonas que tenga alrededor: el
        // conjunto es de quien llega primero (ocupar una pieza lo ocupa todo)
        foreach (var s in _spots)
            if (s != null) s.linkedSpots.Clear();

        float enlace2 = setLinkRadius * setLinkRadius;
        foreach (var sombra in _spots)
        {
            if (sombra == null || sombra.kind != PlayeroSpot.Kind.Sombra) continue;
            foreach (var tumbona in _spots)
            {
                if (tumbona == null || tumbona.kind != PlayeroSpot.Kind.Tumbona) continue;
                if ((tumbona.transform.position - sombra.transform.position).sqrMagnitude > enlace2) continue;
                if (!sombra.linkedSpots.Contains(tumbona)) sombra.linkedSpots.Add(tumbona);
                if (!tumbona.linkedSpots.Contains(sombra)) tumbona.linkedSpots.Add(sombra);
            }
        }

        Debug.Log($"[PlayeroSpawner] {props.Count} props en {clusters.Count} zonas: {_spots.Count} sitios ({nuevos} nuevos).");
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private PlayeroSpot CreateSpot(Transform prop, PlayeroSpot.Kind kind, Vector3 clusterCenter)
    {
        GameObject go = new(kind == PlayeroSpot.Kind.Tumbona ? "SitioTumbona" : "SitioSombra");
        go.transform.SetParent(prop, false);

        if (kind == PlayeroSpot.Kind.Tumbona)
        {
            // el asiento: centro visual de la hamaca, orientado a lo largo de
            // su eje largo. Los props vienen con rotaciones raras de importación,
            // así que se saca el eje del AABB en mundo (el más largo de X/Z) y
            // se alinea con el eje local X del modelo.
            Renderer r = prop.GetComponentInChildren<Renderer>();
            Vector3 pos = r != null ? r.bounds.center : prop.position;
            pos += Vector3.up * loungeHeightOffset;
            go.transform.position = pos;

            Vector3 eje = Vector3.forward;
            if (r != null)
            {
                Vector3 localX = prop.TransformDirection(Vector3.right);
                localX.y = 0f;
                eje = localX.sqrMagnitude < 0.001f
                    ? Vector3.forward
                    : (Mathf.Abs(localX.x) >= Mathf.Abs(localX.z) ? Vector3.right : Vector3.forward);
            }
            go.transform.rotation = Quaternion.LookRotation(eje, Vector3.up);
        }
        else
        {
            // de pie junto al mástil, mirando hacia fuera de la zona
            Vector3 outward = (prop.position - clusterCenter);
            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.back;
            go.transform.SetPositionAndRotation(prop.position + outward * 0.8f, Quaternion.LookRotation(outward, Vector3.up));

        }

        PlayeroSpot spot = go.AddComponent<PlayeroSpot>();
        spot.kind = kind;
        return spot;
    }

    private static bool StartsWith(string name, string prefix)
        => !string.IsNullOrEmpty(prefix) && name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoints[Random.Range(0, spawnPoints.Length)].position, 0.4f);
            Gizmos.DrawRay(spawnPoints[Random.Range(0, spawnPoints.Length)].position, (exitPoint[Random.Range(0, exitPoint.Length)].position - spawnPoints[Random.Range(0, spawnPoints.Length)].position).normalized * 1.5f);
        }
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        foreach (Vector3 a in _anchors)
            Gizmos.DrawWireCube(a + Vector3.up * 0.5f, new Vector3(strollRadius * 2f, 1f, strollRadius * 2f));
    }
}
