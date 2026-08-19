using UnityEngine;
using System.Collections;

/// <summary>
/// cae del cielo, se queda en el suelo un rato y se seca sola. Si la pisas ANTES de que se seque, tu movimiento se
/// vuelve errático (alterna entre muy rápido y muy lento).
///
/// - Con la FREGONA en la mano: pasar por encima (o pulsar E cerca) la limpia
///   mucho más rápido de lo que tardaría en secarse, sin sufrir el efecto.
/// La instancia GaviotaEventManager; no hace falta colocar nada a mano.
/// </summary>
public class CacaGaviota : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float _dryTimeSeconds = 20f;
    [SerializeField] private float _fadeOutTime = 1f;

    [Header("Al pisarla (sin fregona)")]
    [Tooltip("Segundos de movimiento errático.")]
    [SerializeField] private float _erraticDuration = 4f;
    [Tooltip("Multiplicador de velocidad en la fase rápida.")]
    [SerializeField] private float _fastMultiplier = 2.2f;
    [Tooltip("Multiplicador de velocidad en la fase lenta.")]
    [SerializeField] private float _slowMultiplier = 0.35f;
    [Tooltip("Cada cuánto alterna entre rápido y lento.")]
    [SerializeField] private float _switchInterval = 0.35f;
    [SerializeField] private string _zapatillasItemName = "Zapatillas";

    [Header("Limpieza con fregona")]
    [Tooltip("Duración de la limpieza exprés (mucho menor que el secado natural).")]
    [SerializeField] private float _mopCleanTime = 0.4f;

    [Header("Impacto directo (plato perdido)")]
    [Tooltip("Radio alrededor del punto de aterrizaje en el que, si estás con un plato, lo pierdes.")]
    [SerializeField] private float _directHitRadius = 0.9f;

    private bool _landed   = false; // no interactúa hasta aterrizar
    private bool _finished = false; // ya pisada/limpiada/secada


    /// <summary>Anima la caída del regalito hasta groundPos y activa la caca al aterrizar.</summary>
    public void IniciarCaida(Vector3 groundPos, float dropHeight, float fallTime)
    {
        SetCollidersEnabled(false); 
        transform.position = groundPos + Vector3.up * dropHeight;
        StartCoroutine(CaidaRoutine(groundPos, fallTime));
    }

    private IEnumerator CaidaRoutine(Vector3 groundPos, float fallTime)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < fallTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fallTime);
            k = k * k; // acelera al caer (gravedad de mentira)
            transform.position = Vector3.Lerp(start, groundPos, k);
            yield return null;
        }

        transform.position = groundPos;
        AudioManager.Instance?.PlaySFX("caca_splat");


        Collider[] hits = Physics.OverlapSphere(groundPos, _directHitRadius);
        foreach (Collider h in hits)
        {
            PlayerController pc = h.GetComponentInParent<PlayerController>();
            if (pc == null) continue;

            if (pc.IsCarryingFood)
            {
                pc.LoseHeldFood();
                _finished = true;

                AudioManager.Instance?.PlaySFX("caca_pisada");
                HUDMessage.Instance?.ShowBad("¡Una gaviota se ha estrellado contra tu plato! Adiós, comida.");

                Desaparecer(0.15f); // la caca se va con el plato, no deja mancha
                yield break;
            }
            break; // te ha caído encima sin plato: aterriza como mancha normal
        }

        // Mini squash de aterrizaje.
        Vector3 baseScale = transform.localScale;
        transform.localScale = new Vector3(baseScale.x * 1.3f, baseScale.y * 0.6f, baseScale.z * 1.3f);
        yield return new WaitForSeconds(0.08f);
        transform.localScale = baseScale;

        _landed = true;
        SetCollidersEnabled(true);

        // Arrancar el secado natural.
        StartCoroutine(SecadoRoutine());
    }

    private IEnumerator SecadoRoutine()
    {
        yield return new WaitForSeconds(_dryTimeSeconds);
        if (!_finished) Desaparecer(_fadeOutTime);
    }


    void OnTriggerStay(Collider other)
    {
        if (!_landed || _finished) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // Con la fregona: pasar por encima la limpia rápido, sin castigo.
        if (FregonaPickup.IsPlayerCarrying)
        {
            LimpiarConFregona();
            return;
        }

        // Zapatillas anti-gaviotas (mejora del GDD): inmune, la caca se queda.
        if (!string.IsNullOrEmpty(_zapatillasItemName) &&
            OwnedItemsManager.Instance != null &&
            OwnedItemsManager.Instance.GetCount(_zapatillasItemName) > 0)
        {
            return;
        }

        Pisar(player);
    }

    private void Pisar(PlayerController player)
    {
        _finished = true;

        MovimientoErratico.Aplicar(player, _erraticDuration,
                                   _fastMultiplier, _slowMultiplier, _switchInterval);

        AudioManager.Instance?.PlaySFX("caca_pisada");
        HUDMessage.Instance?.ShowBad("¡Has pisado una caca de gaviota! ¡PUAJ!");

        Desaparecer(0.2f); // se te queda pegada en la suela, claro
    }

    /// <summary>Limpieza exprés (fregona). También la llama PlayerController al pulsar E cerca.</summary>
    public void LimpiarConFregona()
    {
        if (!_landed || _finished) return;
        _finished = true;

        AudioManager.Instance?.PlaySFX("fregona_limpiar");
        Desaparecer(_mopCleanTime);
    }

    private void Desaparecer(float duracion)
    {
        StopAllCoroutines();
        SetCollidersEnabled(false);
        StartCoroutine(EncogerYMorir(duracion));
    }

    private IEnumerator EncogerYMorir(float duracion)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / Mathf.Max(0.01f, duracion));
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = enabled;
    }
}


public class MovimientoErratico : MonoBehaviour
{
    private PlayerController _player;
    private float _originalSpeed;
    private float _timeLeft;
    private float _switchTimer;
    private bool  _fastPhase;
    private float _fastMul, _slowMul, _switchInterval;

    public static void Aplicar(PlayerController player, float duration,
                               float fastMul, float slowMul, float switchInterval)
    {
        if (player == null) return;

        MovimientoErratico e = player.GetComponent<MovimientoErratico>();
        if (e == null)
        {
            e = player.gameObject.AddComponent<MovimientoErratico>();
            e._player        = player;
            e._originalSpeed = player.speed; // capturar SOLO la primera vez
        }

        e._fastMul        = fastMul;
        e._slowMul        = slowMul;
        e._switchInterval = switchInterval;
        e._timeLeft       = duration; // repisar refresca, no acumula
    }

    void Update()
    {
        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            Destroy(this);
            return;
        }

        _switchTimer -= Time.deltaTime;
        if (_switchTimer <= 0f)
        {
            _fastPhase    = !_fastPhase;
            _switchTimer  = _switchInterval;
            _player.speed = _originalSpeed * (_fastPhase ? _fastMul : _slowMul);
        }
    }

    void OnDestroy()
    {
        if (_player != null) _player.speed = _originalSpeed;
    }
}
