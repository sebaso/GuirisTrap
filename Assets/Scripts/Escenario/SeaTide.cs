using UnityEngine;


[DisallowMultipleComponent]
public class SeaTide : MonoBehaviour
{
    [Header("Marea principal")]
    [SerializeField] private float _amplitud = 0.12f;
    [SerializeField] private float _periodo = 9f;

    [Header("Onda secundaria")]
    [SerializeField] private float _amplitud2 = 0.04f;
    [SerializeField] private float _periodo2 = 3.7f;

    [Header("Vaivén lateral (opcional)")]
    [SerializeField] private float _amplitudLateral = 0.05f;
    [SerializeField] private float _periodoLateral = 13f;

    [Header("Arranque")]
    [SerializeField] private float _desfase = 0f;

    private Vector3 _posInicial;

    void Awake()
    {
        _posInicial = transform.position;
    }

    void OnDisable()
    {
        transform.position = _posInicial;
    }

    void Update()
    {
        float t = Time.time + _desfase;

        float y = Onda(t, _amplitud, _periodo)
                + Onda(t, _amplitud2, _periodo2);

        float x = Onda(t, _amplitudLateral, _periodoLateral);

        transform.position = _posInicial + new Vector3(x, y, 0f);
    }

    private static float Onda(float t, float amplitud, float periodo)
    {
        if (Mathf.Approximately(amplitud, 0f) || periodo <= 0.01f) return 0f;
        return Mathf.Sin(t * (2f * Mathf.PI / periodo)) * amplitud;
    }

    // Para poder ver el recorrido en el editor sin darle al play.
    void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? _posInicial : transform.position;
        float total = Mathf.Abs(_amplitud) + Mathf.Abs(_amplitud2);

        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        Gizmos.DrawLine(centro + Vector3.up * total, centro - Vector3.up * total);
        Gizmos.DrawWireCube(centro + Vector3.up * total, new Vector3(2f, 0.01f, 2f));
        Gizmos.DrawWireCube(centro - Vector3.up * total, new Vector3(2f, 0.01f, 2f));
    }
}
