using UnityEngine;

// Sonido ambiental por zonas: el BoxCollider define el área (ajustable en el
// editor) y el volumen sube/baja según la distancia del jugador al borde.
// Sobre el loop base puede haber capas de sonidos puntuales (gaviotas,
// campanas...) que se disparan por probabilidad con cooldown.
[RequireComponent(typeof(AudioSource), typeof(BoxCollider))]
public class AmbientZone : MonoBehaviour
{
    [Tooltip("Volumen cuando el jugador está dentro del área.")]
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Tooltip("Hasta qué distancia fuera del área se sigue oyendo, desvaneciéndose hasta 0.")]
    public float fadeDistance = 8f;
    [Tooltip("Segundos que tarda el volumen en llegar a su objetivo (evita cortes al cruzar el borde).")]
    public float fadeTime = 1.5f;
    [Tooltip("0 = ambiental 2D (recomendado para ambientes), 1 = totalmente 3D.")]
    [Range(0f, 1f)] public float spatialBlend = 0f;
    [Tooltip("Sonidos puntuales sobre el loop: cada 'interval' segundos hay 'chance' de que suene uno; tras sonar, guarda 'cooldown' de silencio. Heredan el fade de la zona.")]
    public AmbientLayer[] layers;

    [System.Serializable]
    public class AmbientLayer
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float chance = 0.15f; // probabilidad por intento
        public float interval = 1f;                  // segundos entre intentos
        public float cooldown = 30f;                 // silencio tras sonar
        [Range(0f, 1f)] public float volume = 1f;
    }

    private AudioSource _source;
    private BoxCollider _area;
    private Transform _player;
    private float[] _layerNextRoll;      // próximo intento de cada capa
    private float[] _layerCooldownUntil; // fin del cooldown de cada capa

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _area = GetComponent<BoxCollider>();
        // El trigger es inerte: nadie escucha sus eventos, solo define el área.
        _area.isTrigger = true;

        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = spatialBlend;
        _source.volume = 0f;
        if (_source.clip != null) _source.Play();

        int n = layers != null ? layers.Length : 0;
        _layerNextRoll = new float[n];
        _layerCooldownUntil = new float[n];
        // Desfase inicial: zonas con capas iguales no tiran los dados a la vez.
        for (int i = 0; i < n; i++)
            _layerNextRoll[i] = Random.Range(0f, Mathf.Max(0.05f, layers[i].interval));
    }

    void Update()
    {
        if (_player == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) return;
            _player = pc.transform;
        }

        // 0 dentro del área; 1 en fadeDistance más allá del borde.
        float d = Vector3.Distance(_player.position, _area.ClosestPoint(_player.position));
        float target = maxVolume * (1f - Mathf.Clamp01(d / fadeDistance));
        _source.volume = Mathf.MoveTowards(_source.volume, target, Time.deltaTime / fadeTime * maxVolume);

        UpdateLayers();
    }

    void UpdateLayers()
    {
        if (layers == null || _source.volume <= 0.001f) return; // nadie lo oye: no tirar dados

        for (int i = 0; i < layers.Length; i++)
        {
            AmbientLayer l = layers[i];
            if (l == null || l.clip == null) continue;

            if (Time.time < _layerNextRoll[i]) continue;
            _layerNextRoll[i] = Time.time + Mathf.Max(0.05f, l.interval);

            if (Time.time < _layerCooldownUntil[i]) continue;
            if (Random.value < l.chance)
            {
                // PlayOneShot pasa por el volumen de la fuente: hereda el fade de la zona.
                _source.PlayOneShot(l.clip, l.volume);
                _layerCooldownUntil[i] = Time.time + Mathf.Max(0f, l.cooldown);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider area = _area != null ? _area : GetComponent<BoxCollider>();
        if (area == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
