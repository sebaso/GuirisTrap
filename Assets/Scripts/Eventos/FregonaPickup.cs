using UnityEngine;


public class FregonaPickup : MonoBehaviour
{
    /// <summary>La fregona que el jugador lleva ahora mismo (null si ninguna).</summary>
    public static FregonaPickup Carried { get; private set; }

    /// <summary>¿Lleva el jugador una fregona? (Lo consulta CacaGaviota.)</summary>
    public static bool IsPlayerCarrying => Carried != null;

    [Header("Al llevarla (enganchada al jugador)")]
    [SerializeField] private Vector3 _carryLocalOffset = new Vector3(-0.35f, 1.0f, 0.3f);
    [SerializeField] private Vector3 _carryLocalEuler  = new Vector3(0f, 0f, 25f);

    [Header("Vuelta al soporte")]
    [Tooltip("Duración de la animación de regreso al soporte.")]
    [SerializeField] private float _returnLerpTime = 0.3f;

    /// <summary>¿Está esta fregona concreta siendo llevada?</summary>
    public bool IsCarried { get; private set; }

    private FregonaSoporte _holder;
    private Transform _restAnchor;

    // Pose EN REPOSO respecto al anclaje, capturada tal como se autoró.
    private Vector3    _restLocalPos   = Vector3.zero;
    private Quaternion _restLocalRot   = Quaternion.identity;
    private Vector3    _restLocalScale = Vector3.one;

    private Collider[] _colliders;

    void Awake()
    {
        _colliders = GetComponentsInChildren<Collider>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    void OnDestroy()
    {
        if (Carried == this) Carried = null;
    }

    /// <summary>Lo llama FregonaSoporte al inicializarse: registra dónde descansa y su pose exacta.</summary>
    public void AttachToHolder(FregonaSoporte holder, Transform restAnchor)
    {
        _holder     = holder;
        _restAnchor = restAnchor;

        if (transform.parent != restAnchor)
            transform.SetParent(restAnchor, worldPositionStays: true);

        _restLocalPos   = transform.localPosition;
        _restLocalRot   = transform.localRotation;
        _restLocalScale = transform.localScale;
    }

    // ------------------------------------------------------------------
    //  Coger
    // ------------------------------------------------------------------

    /// <summary>Coge la fregona y la engancha al jugador (lo llama PlayerController).</summary>
    public void TryPickUp(PlayerController player)
    {
        if (IsCarried || player == null) return;
        if (Carried != null) return; // ya lleva una

        if (_restAnchor == null)
        {
            Debug.LogWarning("[FregonaPickup] Cogida sin soporte registrado. " +
                             "¿Falta el componente FregonaSoporte en el mueble? Capturando pose actual como hogar.");
            _restAnchor     = transform.parent;
            _restLocalPos   = transform.localPosition;
            _restLocalRot   = transform.localRotation;
            _restLocalScale = transform.localScale;
        }

        IsCarried = true;
        Carried   = this;

        SetCollidersEnabled(false);

        // worldPositionStays: TRUE conserva el tamaño de mundo (anti-Pokéball).
        transform.SetParent(player.transform, worldPositionStays: true);
        transform.localPosition = _carryLocalOffset;
        transform.localRotation = Quaternion.Euler(_carryLocalEuler);

        AudioManager.Instance?.PlaySFX("fregona_pickup");
        HUDMessage.Instance?.ShowGood("¡Fregona en mano! Pasa por encima de las cacas para limpiarlas.");
    }


    /// <summary>Devuelve la fregona a su soporte, a la pose exacta autorada.</summary>
    public bool ReturnToHolder()
    {
        IsCarried = false;
        if (Carried == this) Carried = null;

        if (_restAnchor == null)
        {
            FregonaSoporte nearest = FindNearestSoporte();
            if (nearest != null)
            {
                Debug.LogWarning("[FregonaPickup] Anclaje perdido: adoptando el soporte más cercano " +
                                 $"('{nearest.name}'). Revisa que AttachToHolder se esté llamando.");
                _restAnchor   = nearest.RestAnchor;
                _restLocalPos = Vector3.zero;
                _restLocalRot = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning("[FregonaPickup] Sin soporte al que volver (ninguno en escena). " +
                                 "Soltando la fregona donde está.");
                transform.SetParent(null, true);
                SetCollidersEnabled(true);
                return false;
            }
        }

        transform.SetParent(_restAnchor, worldPositionStays: true);
        StopAllCoroutines();
        StartCoroutine(ReturnRoutine());
        return true;
    }

    private System.Collections.IEnumerator ReturnRoutine()
    {
        Vector3    startPos   = transform.position;
        Quaternion startRot   = transform.rotation;
        Vector3    startScale = transform.localScale;
        float t = 0f;

        SetCollidersEnabled(false);

        while (t < _returnLerpTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / _returnLerpTime);

            Vector3    targetPos = _restAnchor.TransformPoint(_restLocalPos);
            Quaternion targetRot = _restAnchor.rotation * _restLocalRot;

            transform.position   = Vector3.Lerp(startPos, targetPos, k);
            transform.rotation   = Quaternion.Slerp(startRot, targetRot, k);
            transform.localScale = Vector3.Lerp(startScale, _restLocalScale, k);
            yield return null;
        }

        transform.localPosition = _restLocalPos;
        transform.localRotation = _restLocalRot;
        transform.localScale    = _restLocalScale;

        SetCollidersEnabled(true);
    }

    private FregonaSoporte FindNearestSoporte()
    {
        FregonaSoporte[] all = FindObjectsByType<FregonaSoporte>(FindObjectsSortMode.None);
        FregonaSoporte best = null;
        float bestDist = float.MaxValue;

        foreach (FregonaSoporte s in all)
        {
            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null) return;
        foreach (Collider c in _colliders)
            if (c != null) c.enabled = enabled;
    }
}
