using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FadeEffectComponent : MonoBehaviour
{
    private static readonly int AlphaProperty = Shader.PropertyToID("_alpha");

    [SerializeField] 
    private float _fadeSpeed = 5f;
    [SerializeField] 
    private float _minAlpha  = 0.15f;
    [SerializeField] 
    private float _maxAlpha  = 0.4f;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private float _currentAlpha;
    private bool _isOccluding;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        if (_renderer != null && _renderer.sharedMaterial != null && !_renderer.sharedMaterial.HasProperty(AlphaProperty))
            Debug.LogWarning($"[FadeEffectComponent] El material de '{name}' no tiene una propiedad float '_alpha'. Este efecto no va a hacer nada visible.", this);

        _currentAlpha = _maxAlpha;
        ApplyAlpha();
    }

    void Update()
    {
        float target = _isOccluding ? _minAlpha : _maxAlpha;
        if (Mathf.Approximately(_currentAlpha, target)) return;

        _currentAlpha = Mathf.MoveTowards(_currentAlpha, target, _fadeSpeed * Time.deltaTime);
        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(AlphaProperty, _currentAlpha);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    public void SetIsOccluding(bool occluding) => _isOccluding = occluding;
}