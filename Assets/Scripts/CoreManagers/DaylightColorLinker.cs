using UnityEngine;

public class DaylightColorLinker : MonoBehaviour
{

    public Light _directLight;
    private Light spotlight;
    public float tickUpdateLight = 0.1f;
    public float intensityMultiplier = 2.0f;
    private float _timer;
    private void Awake()
    {
        spotlight = GetComponentInChildren<Light>();
    }

    public void ApplyColor(Color color, Light lightToUpdate)
    {
        _directLight.color = color;
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
