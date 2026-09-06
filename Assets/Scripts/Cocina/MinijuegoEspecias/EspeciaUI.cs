using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Cada especia sigue su propio camino de waypoints en orden.
// Al llegar al final (o al rebotar contra un CuboNegro / especia congelada)
// invierte el recorrido. Los rebotes por colisión los detecta EspeciasMinigame
// por flanco (solo al ENTRAR en contacto), así que aquí ya no hace falta cooldown.
public class EspeciaUI : MonoBehaviour
{
    [Header("Camino")]
    public List<RectTransform> camino = new List<RectTransform>();

    [Header("Feedback al congelar")]
    public Color frozenTint = new Color(0.55f, 0.85f, 1f, 1f);
    public float freezePopScale = 1.3f;
    public float freezePopTime = 0.18f;

    [Header("Desvanecer tras congelar")]
    public float vanishDelay = 2f;
    [Tooltip("Lo que tarda en desvanecerse una vez le toca irse.")]
    public float vanishFadeTime = 0.35f;

    [HideInInspector] public float speed = 100f; // asignado por EspeciasMinigame

    private RectTransform _rect;
    private Graphic       _graphic;
    private Color         _originalColor;
    private Vector3       _baseScale = Vector3.one;

    private int  _wpIndex   = 0;  // waypoint actual
    private int  _dir       = 1;  // 1 = avanzar, -1 = retroceder
    private bool _congelada = false;
    private bool _desvanecida = false;
    private Coroutine _popCo;
    private Coroutine _vanishCo;

    public bool IsCongelada => _congelada;

    /// <summary>Ya se ha ido: ni bloquea balas ni hace rebotar a nadie.</summary>
    public bool IsDesvanecida => _desvanecida;

    public RectTransform Rect => _rect;

    void Awake()
    {
        _rect    = GetComponent<RectTransform>();
        _graphic = GetComponent<Graphic>();
        if (_graphic != null) _originalColor = _graphic.color;
        if (_rect    != null) _baseScale     = _rect.localScale;
    }

    void Start()
    {
        if (camino.Count > 0 && _rect != null)
            _rect.localPosition = camino[0].localPosition;
    }

    void Update()
    {
        if (_congelada || camino.Count < 2 || _rect == null) return;

        RectTransform target = camino[_wpIndex];
        _rect.localPosition = Vector3.MoveTowards(
            _rect.localPosition,
            target.localPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(_rect.localPosition, target.localPosition) < 1f)
            AdvanceWaypoint();
    }

    private void AdvanceWaypoint()
    {
        int next = _wpIndex + _dir;

        // Rebote en los extremos del camino
        if (next >= camino.Count || next < 0)
        {
            _dir *= -1;
            next  = _wpIndex + _dir;
        }

        _wpIndex = next;
    }

    // Llamado por EspeciasMinigame al impactar una bala
    public void Congelar()
    {
        if (_congelada) return;
        _congelada = true;

        // Feedback visual: tinte azul hielo + pop de escala.
        if (_graphic != null) _graphic.color = frozenTint;

        if (_popCo != null) StopCoroutine(_popCo);
        if (isActiveAndEnabled && _rect != null)
            _popCo = StartCoroutine(FreezePopRoutine());

        if (_vanishCo != null) StopCoroutine(_vanishCo);
        if (isActiveAndEnabled)
            _vanishCo = StartCoroutine(VanishRoutine());
    }

    private IEnumerator VanishRoutine()
    {
        if (vanishDelay > 0f) yield return new WaitForSeconds(vanishDelay);

        _desvanecida = true;

        Color from = _graphic != null ? _graphic.color : Color.white;
        float t = 0f;

        while (t < vanishFadeTime && _graphic != null)
        {
            t += Time.deltaTime;
            Color c = from;
            c.a = Mathf.Lerp(from.a, 0f, t / vanishFadeTime);
            _graphic.color = c;
            yield return null;
        }

        if (_graphic != null)
        {
            Color c = from; c.a = 0f;
            _graphic.color = c;
        }

        _vanishCo = null;
    }

    private IEnumerator FreezePopRoutine()
    {
        float half = Mathf.Max(0.01f, freezePopTime * 0.5f);
        float t = 0f;

        // Crece...
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(1f, freezePopScale, t / half);
            _rect.localScale = _baseScale * k;
            yield return null;
        }

        // ...y vuelve.
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(freezePopScale, 1f, t / half);
            _rect.localScale = _baseScale * k;
            yield return null;
        }

        _rect.localScale = _baseScale;
        _popCo = null;
    }

    // Rebote (llamado desde EspeciasMinigame solo al ENTRAR en contacto)
    public void Rebotar()
    {
        if (_congelada) return;
        _dir     *= -1;
        _wpIndex  = Mathf.Clamp(_wpIndex + _dir, 0, camino.Count - 1);
    }

    public void Resetear()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();

        if (_popCo != null)   { StopCoroutine(_popCo);   _popCo = null; }
        if (_vanishCo != null) { StopCoroutine(_vanishCo); _vanishCo = null; }

        _congelada   = false;
        _desvanecida = false;
        _dir       = 1;
        _wpIndex   = 0;

        if (_graphic != null) _graphic.color   = _originalColor;
        if (_rect    != null) _rect.localScale = _baseScale;

        if (camino.Count > 0 && _rect != null)
            _rect.localPosition = camino[0].localPosition;
    }
}