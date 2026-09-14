using UnityEngine;

// Vida para el mar: desplaza la textura del agua y balancea el plano en Y
// (amplitud pequeña para que la orilla no se separe de la arena). Cada plano
// lleva su fase para que no se muevan al unísono.
[RequireComponent(typeof(Renderer))]
public class WaterDrift : MonoBehaviour
{
    [Tooltip("Desplazamiento de la textura por segundo.")]
    public Vector2 scrollSpeed = new(0.012f, 0.008f);
    [Tooltip("Amplitud del balanceo vertical (metros).")]
    public float bobAmplitude = 0.05f;
    [Tooltip("Segundos que dura un balanceo completo.")]
    public float bobPeriod = 7f;
    [Tooltip("Fase del balanceo; -1 = aleatoria al arrancar.")]
    public float phase = -1f;

    private Material _material;
    private Vector2 _offset;
    private float _baseY;
    private float _phase;

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null && r.sharedMaterial.mainTexture != null)
        {
            // instancia del material: solo afecta a este render en runtime
            _material = r.material;
            _offset = _material.mainTextureOffset;
        }
        _baseY = transform.position.y;
        _phase = phase >= 0f ? phase : Random.value;
    }

    void Update()
    {
        if (_material != null)
        {
            _offset += scrollSpeed * Time.deltaTime;
            _material.mainTextureOffset = _offset;
        }

        float t = (Time.time / Mathf.Max(0.1f, bobPeriod) + _phase) * Mathf.PI * 2f;
        Vector3 p = transform.position;
        p.y = _baseY + Mathf.Sin(t) * bobAmplitude;
        transform.position = p;
    }
}
