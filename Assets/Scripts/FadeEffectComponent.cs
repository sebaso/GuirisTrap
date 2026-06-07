using Unity.VisualScripting;
using UnityEngine;

public class FadeEffectComponent : MonoBehaviour
{
    [SerializeField] private float _fadeSpeed = 5f;
    [SerializeField] private float _minAlpha  = 0.15f;
    [SerializeField] private float _maxAlpha  = 0.4f;
    private Renderer _renderer;
    private bool _isOcclusing = false;

    void Awake()
    {
        _renderer = gameObject.GetComponent<Renderer>();
    }

    void FixedUpdate()
    {
        float currentAlpha = _renderer.material.GetFloat("_alpha");
        if(_isOcclusing && currentAlpha > _minAlpha )
            ApplyFadeOn();
        else if(!_isOcclusing && currentAlpha < _maxAlpha)
            ApplyFadeOut();
    }
    public void ApplyFadeOn()
    {
        float currentAlpha = _renderer.material.GetFloat("_alpha");
        currentAlpha -= _fadeSpeed * Time.deltaTime;
        currentAlpha = Mathf.Clamp01(currentAlpha);
        _renderer.material.SetFloat("_alpha", currentAlpha);
    }
    public void ApplyFadeOut()
    {
        float currentAlpha = _renderer.material.GetFloat("_alpha");
        currentAlpha += _fadeSpeed * Time.deltaTime;
        currentAlpha = Mathf.Clamp01(currentAlpha);
        _renderer.material.SetFloat("_alpha", currentAlpha);
    }
    public void SetIsOcclusing(bool occlusing){ _isOcclusing = occlusing; }
}