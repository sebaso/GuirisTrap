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

    [HideInInspector] public float speed = 100f; // asignado por EspeciasMinigame

    private RectTransform _rect;
    private Graphic       _graphic;
    private Color         _originalColor;
    private Vector3       _baseScale = Vector3.one;

    private int  _wpIndex   = 0;  // waypoint actual
    private int  _dir       = 1;  // 1 = avanzar, -1 = retroceder
    private bool _congelada = false;
    private Coroutine _popCo;

    public bool IsCongelada => _congelada;
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

        if (_popCo != null) { StopCoroutine(_popCo); _popCo = null; }

        _congelada = false;
        _dir       = 1;
        _wpIndex   = 0;

        if (_graphic != null) _graphic.color   = _originalColor;
        if (_rect    != null) _rect.localScale = _baseScale;

        if (camino.Count > 0 && _rect != null)
            _rect.localPosition = camino[0].localPosition;
    }
}