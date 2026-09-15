using TMPro;
using UnityEngine;



public class DayArcClock : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform _pivoteAguja;
    [SerializeField] private TMP_Text _textoTiempo;

    [Header("Recorrido de la aguja")]
    [SerializeField] private float _anguloInicio = 78f;
    [SerializeField] private float _anguloFin = -78f;
    [SerializeField] private float _suavizado = 6f;

    [Header("Aviso de final de día")]
    [Range(0f, 1f)]
    [SerializeField] private float _umbralAviso = 0.85f;
    [SerializeField] private float _temblorGrados = 2.5f;
    [SerializeField] private float _temblorVelocidad = 22f;

    [Header("Previsualización (solo editor)")]
    [Range(0f, 1f)]
    [SerializeField] private float _previewProgreso = 0f;

    private float _anguloActual;
    private bool _iniciado;

#if UNITY_EDITOR
    void OnValidate()
    {
        // Solo fuera de play: durante la partida manda el DayManager.
        if (!Application.isPlaying) Aplicar(AnguloPara(_previewProgreso));
    }
#endif

    void OnEnable()
    {
        // Colocarla ya en su sitio, para que no se vea barrer desde 0 al abrir.
        _anguloActual = AnguloPara(ProgresoActual());
        Aplicar(_anguloActual);
        _iniciado = true;
    }

    void Update()
    {
        float objetivo = AnguloPara(ProgresoActual());

        if (!_iniciado || _suavizado <= 0f)
        {
            _anguloActual = objetivo;
            _iniciado = true;
        }
        else
        {
            _anguloActual = Mathf.Lerp(_anguloActual, objetivo,
                                       1f - Mathf.Exp(-_suavizado * Time.deltaTime));
        }

        float mostrar = _anguloActual;

        float p = ProgresoActual();
        if (p >= _umbralAviso && p < 1f)
        {
            float fuerza = Mathf.InverseLerp(_umbralAviso, 1f, p);
            mostrar += Mathf.Sin(Time.time * _temblorVelocidad) * _temblorGrados * fuerza;
        }

        Aplicar(mostrar);
        ActualizarTexto();
    }

    // ------------------------------------------------------------------

    private float ProgresoActual()
    {
        if (DayManager.Instance == null) return 0f;
        return Mathf.Clamp01(DayManager.Instance.DayProgress);
    }

    private float AnguloPara(float progreso) => Mathf.Lerp(_anguloInicio, _anguloFin, progreso);

    private void Aplicar(float angulo)
    {
        if (_pivoteAguja != null)
            _pivoteAguja.localRotation = Quaternion.Euler(0f, 0f, angulo);
    }

    private void ActualizarTexto()
    {
        if (_textoTiempo == null || DayManager.Instance == null) return;

        float restante = Mathf.Max(0f, DayManager.Instance.TimeRemaining);
        int min = Mathf.FloorToInt(restante / 60f);
        int seg = Mathf.FloorToInt(restante % 60f);

        _textoTiempo.text = $"{min}:{seg:00}";
    }

    void OnDrawGizmosSelected()
    {
        if (_pivoteAguja == null) return;

        Vector3 centro = _pivoteAguja.position;
        float radio = 60f;

        Gizmos.color = Color.yellow;
        Dibuja(centro, _anguloInicio, radio);
        Gizmos.color = new Color(0.6f, 0.6f, 1f);
        Dibuja(centro, _anguloFin, radio);
    }

    private void Dibuja(Vector3 centro, float angulo, float radio)
    {
        float rad = (angulo + 90f) * Mathf.Deg2Rad;
        Gizmos.DrawLine(centro, centro + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radio);
    }
}