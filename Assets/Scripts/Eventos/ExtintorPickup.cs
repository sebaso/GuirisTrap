using UnityEngine;


public class ExtintorPickup : MonoBehaviour
{
    /// <summary>El extintor que el jugador lleva encima ahora mismo (null si ninguno).</summary>
    public static ExtintorPickup Carried { get; private set; }

    /// <summary>¿Lleva el jugador un extintor encima? (Lo consulta IncendioMinigame.)</summary>
    public static bool IsPlayerCarrying => Carried != null;

    [Header("Al llevarlo (enganchado al jugador)")]
    [Tooltip("Posición local respecto al jugador. Por defecto, en la mano/delante.")]
    [SerializeField] private Vector3 _carryLocalOffset = new Vector3(0.35f, 1.1f, 0.35f);
    [SerializeField] private Vector3 _carryLocalEuler  = new Vector3(0f, 0f, -20f);

    [Header("Vuelta al soporte")]
    [Tooltip("Duración de la animación de regreso al soporte.")]
    [SerializeField] private float _returnLerpTime = 0.3f;

    /// <summary>¿Está este extintor concreto siendo llevado?</summary>
    public bool IsCarried { get; private set; }

    private ExtintorSoporte _holder;
    private Transform _restAnchor;

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
            //Debug.Log("[ExtintorPickup] Rigidbody detectado: puesto en kinematic.");
        }
    }

    void OnDestroy()
    {
        if (Carried == this) Carried = null;
    }


    public void AttachToHolder(ExtintorSoporte holder, Transform restAnchor)
    {
        _holder     = holder;
        _restAnchor = restAnchor;

        // Reparentar al anclaje si no lo estaba ya (sin mover nada en mundo),
        // para que la pose local capturada sea respecto al anclaje real.
        if (transform.parent != restAnchor)
            transform.SetParent(restAnchor, worldPositionStays: true);

        _restLocalPos   = transform.localPosition;
        _restLocalRot   = transform.localRotation;
        _restLocalScale = transform.localScale;
    }



    /// <summary>Coge el extintor y lo engancha al jugador (lo llama PlayerController).</summary>
    public void TryPickUp(PlayerController player)
    {
        if (IsCarried || player == null) return;
        if (Carried != null) return; // el jugador ya lleva uno

        if (_restAnchor == null)
        {

            Debug.Log("[ExtintorPickup] Cogido sin soporte registrado.");
            _restAnchor     = transform.parent;
            _restLocalPos   = transform.localPosition;
            _restLocalRot   = transform.localRotation;
            _restLocalScale = transform.localScale;
        }

        IsCarried = true;
        Carried   = this;

        SetCollidersEnabled(false); 
        transform.SetParent(player.transform, worldPositionStays: true);
        transform.localPosition = _carryLocalOffset;
        transform.localRotation = Quaternion.Euler(_carryLocalEuler);

        AudioManager.Instance?.PlaySFX("extintor_pickup");
    }

    // ------------------------------------------------------------------
    //  Volver al soporte
    // ------------------------------------------------------------------

    /// <summary>
    /// Devuelve el extintor a su soporte (lo llaman IncendioMinigame al
    /// terminar y PlayerController al devolverlo con E). Vuelve a la pose
    /// exacta en la que estaba autorado. True si tenía un hogar al que volver.
    /// </summary>
    public bool ReturnToHolder()
    {
        IsCarried = false;
        if (Carried == this) Carried = null;

        // Autocuración: si el anclaje se perdió (soporte destruido, referencia
        // rota tras reestructurar el prefab...), adoptar el soporte más
        // cercano de la escena.
        if (_restAnchor == null)
        {
            ExtintorSoporte nearest = FindNearestSoporte();
            if (nearest != null)
            {
                Debug.Log("[ExtintorPickup] Anclaje perdido: adoptando el soporte más cercano " +
                                 $"('{nearest.name}'). Revisa que AttachToHolder se esté llamando.");
                _restAnchor = nearest.RestAnchor;
                // Pose de reposo desconocida respecto a este anclaje: usar su origen.
                _restLocalPos   = Vector3.zero;
                _restLocalRot   = Quaternion.identity;
                // _restLocalScale se conserva (la escala de reposo sigue valiendo).
            }
            else
            {
                Debug.LogWarning("[ExtintorPickup] Sin soporte al que volver (ninguno en escena). " +
                                 "Soltando el extintor donde está.");
                transform.SetParent(null, true);
                SetCollidersEnabled(true);
                return false;
            }
        }

        transform.SetParent(_restAnchor, worldPositionStays: true);
        StopAllCoroutines(); // por si había una vuelta anterior a medias
        StartCoroutine(ReturnRoutine());
        return true;
    }

    private System.Collections.IEnumerator ReturnRoutine()
    {
        Vector3    startPos   = transform.position;
        Quaternion startRot   = transform.rotation;
        Vector3    startScale = transform.localScale;
        float t = 0f;

        // Colliders desactivados durante el vuelo para que no se pueda recoger a medio camino.
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

        SetCollidersEnabled(true); // ya se puede volver a coger
    }

    private ExtintorSoporte FindNearestSoporte()
    {
        ExtintorSoporte[] all = FindObjectsByType<ExtintorSoporte>(FindObjectsSortMode.None);
        ExtintorSoporte best = null;
        float bestDist = float.MaxValue;

        foreach (ExtintorSoporte s in all)
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