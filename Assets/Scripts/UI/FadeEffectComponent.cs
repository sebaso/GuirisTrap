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

        if (_renderer != null)
        {
            foreach (Material mat in _renderer.sharedMaterials)
            {
                if (mat != null && !mat.HasProperty(AlphaProperty))
                    Debug.LogWarning($"[FadeEffectComponent] El material '{mat.name}' de '{name}' no tiene una propiedad float '_alpha'. Este efecto no va a hacer nada visible en ese slot.", this);
            }
        }

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
        int count = _renderer.sharedMaterials.Length;
        for (int i = 0; i < count; i++)
        {
            _renderer.GetPropertyBlock(_propertyBlock, i);
            _propertyBlock.SetFloat(AlphaProperty, _currentAlpha);
            _renderer.SetPropertyBlock(_propertyBlock, i);
        }
    }

    public void SetIsOccluding(bool occluding) => _isOccluding = occluding;
}