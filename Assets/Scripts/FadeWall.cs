using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FadeWall : MonoBehaviour
{
    [SerializeField] private float _fadeSpeed = 5f;
    [SerializeField] private float _occludedAlpha = 0.2f;

    private Renderer _renderer;
    private float _targetAlpha = 1f;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void SetOccluded()
    {
        _targetAlpha = _occludedAlpha;
    }

    private void Update()
    {
        Color color = _renderer.material.color;

        color.a = Mathf.Lerp(
            color.a,
            _targetAlpha,
            Time.deltaTime * _fadeSpeed
        );

        _renderer.material.color = color;

        // Si este frame nadie nos ha marcado como ocluidas,
        // volveremos gradualmente a opacas.
        _targetAlpha = 1f;
    }
}