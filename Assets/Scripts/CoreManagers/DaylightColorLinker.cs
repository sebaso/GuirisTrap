using UnityEngine;

public class DaylightColorLinker : MonoBehaviour
{
    [Tooltip("Si se deja vacía se busca sola.")]
    public Light _directLight;

    private Light spotlight;
    public float tickUpdateLight = 0.1f;
    public float intensityMultiplier = 2.0f;
    private float _timer;

    private void Awake()
    {
        spotlight = GetComponentInChildren<Light>();


        if (_directLight == null)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l != null && l.type == LightType.Directional) { _directLight = l; break; }
            }
        }

        if (_directLight == null || spotlight == null)
        {
            Debug.LogWarning($"[DaylightColorLinker] '{name}' se desactiva: " +
                             (_directLight == null ? "no hay luz direccional. " : "") +
                             (spotlight == null ? "no hay ninguna Light hija que teñir." : ""), this);
            enabled = false;
        }
    }

    public void ApplyColor(Color color, Light lightToUpdate)
    {
        if (_directLight == null || lightToUpdate == null) return;

        lightToUpdate.color = color;
        lightToUpdate.intensity = _directLight.intensity * intensityMultiplier;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= tickUpdateLight)
        {
            ApplyColor(_directLight.color, spotlight);
            _timer = 0f;
        }
    }
}