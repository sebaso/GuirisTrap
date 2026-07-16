using UnityEngine;

/// <summary>
/// EXTINTOR COGIBLE (flujo "como un plato", un solo uso por viaje).
///
/// Cuelga de un ExtintorSoporte. El jugador lo coge interactuando (E) y se le
/// engancha (como la comida). Cuando interactúa con una estación EN LLAMAS
/// llevándolo, el minijuego de incendio arranca en modo FÁCIL y, al terminar,
/// el extintor VUELVE SOLO a su soporte (un uso por viaje: para el siguiente
/// incendio hay que ir a por él otra vez).
///
/// A diferencia de la versión anterior (llevarlo todo el día), esto es más
/// táctico y encaja con el "tener a mano" del GDD: el soporte es permanente
/// (comprado y colocado), el extintor es el consumible reutilizable.
/// </summary>
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
    private Collider[] _colliders;

    void Awake()
    {
        _colliders = GetComponentsInChildren<Collider>();
    }

    void OnDestroy()
    {
        if (Carried == this) Carried = null;
    }

    /// <summary>Lo llama ExtintorSoporte al inicializarse: registra dónde descansa.</summary>
    public void AttachToHolder(ExtintorSoporte holder, Transform restAnchor)
    {
        _holder     = holder;
        _restAnchor = restAnchor;
    }

    // ------------------------------------------------------------------
    //  Coger
    // ------------------------------------------------------------------

    /// <summary>Coge el extintor y lo engancha al jugador (lo llama PlayerController).</summary>
    public void TryPickUp(PlayerController player)
    {
        if (IsCarried || player == null) return;
        if (Carried != null) return; // el jugador ya lleva uno

        IsCarried = true;
        Carried   = this;

        SetCollidersEnabled(false); // que no estorbe ni reaparezca en el OverlapSphere

        transform.SetParent(player.transform, worldPositionStays: false);
        transform.localPosition = _carryLocalOffset;
        transform.localRotation = Quaternion.Euler(_carryLocalEuler);

        AudioManager.Instance?.PlaySFX("extintor_pickup");
        HUDMessage.Instance?.ShowGood("¡Extintor en mano! Úsalo en una estación en llamas.");
    }

    // ------------------------------------------------------------------
    //  Volver al soporte (tras usarse en un incendio)
    // ------------------------------------------------------------------

    /// <summary>
    /// Devuelve el extintor a su soporte. Lo llama IncendioMinigame al terminar
    /// un incendio que se apagó con el extintor (modo fácil), gane o pierda:
    /// el extintor es de un solo uso por viaje.
    /// </summary>
    public void ReturnToHolder()
    {
        IsCarried = false;
        if (Carried == this) Carried = null;

        if (_restAnchor == null)
        {
            // Sin anclaje conocido: al menos soltarlo del jugador.
            transform.SetParent(null, true);
            SetCollidersEnabled(true);
            return;
        }

        transform.SetParent(_restAnchor, worldPositionStays: true);
        SetCollidersEnabled(true);

        // Animar la vuelta a su hueco (posición/rotación local cero respecto al anclaje).
        Vector3 targetWorld = _restAnchor.position;
        Quaternion targetRot = _restAnchor.rotation;
        StartCoroutine(ReturnRoutine(targetWorld, targetRot));
    }

    private System.Collections.IEnumerator ReturnRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float t = 0f;

        // Colliders desactivados durante el vuelo para que no se pueda recoger a medio camino.
        SetCollidersEnabled(false);

        while (t < _returnLerpTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / _returnLerpTime);
            transform.position = Vector3.Lerp(startPos, targetPos, k);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        transform.position      = targetPos;
        transform.rotation      = targetRot;
        transform.localPosition = Vector3.zero;

        SetCollidersEnabled(true); // ya se puede volver a coger
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null) return;
        foreach (Collider c in _colliders)
            if (c != null) c.enabled = enabled;
    }
}