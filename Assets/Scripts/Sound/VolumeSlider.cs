using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public enum Channel { Music, Sfx }

    [SerializeField] private Channel _channel = Channel.Music;

    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
    }

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSlider] No hay AudioManager. ¿Está en la primera escena (Menú)?");
            return;
        }

        _slider.value = _channel == Channel.Music
            ? AudioManager.Instance.MusicVolume
            : AudioManager.Instance.SfxVolume;

        _slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        if (AudioManager.Instance == null) return;

        if (_channel == Channel.Music)
            AudioManager.Instance.SetMusicVolume(value);
        else
            AudioManager.Instance.SetSfxVolume(value);
    }
}
