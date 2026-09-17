using UnityEngine;

// Icono 2D de un plato: flota a altura mundial fija sobre un objeto de
// referencia (la bandeja mientras se lleva, el punto de comida una vez servido)
// y mira siempre a la cámara. El offset es en mundo —no en ejes locales— porque
// la bandeja va inclinada en la mano y un offset local se derramaría de lado.
public class FoodIcon : MonoBehaviour
{
    private const float BobAmplitude = 0.03f;
    private const float BobSpeed = 2.2f;

    private SpriteRenderer _sprite;
    private Transform _follow;
    private float _height;
    private float _worldSize;
    private Transform _cam;

    public static FoodIcon Create(Transform followTarget, float worldHeight, float worldSize)
    {
        var go = new GameObject("FoodIcon");
        go.transform.SetParent(followTarget, false);
        var icon = go.AddComponent<FoodIcon>();
        icon._sprite = go.AddComponent<SpriteRenderer>();
        icon._follow = followTarget;
        icon._height = worldHeight;
        icon._worldSize = worldSize;
        go.SetActive(false);
        return icon;
    }

    public void Set(Sprite sprite)
    {
        _sprite.sprite = sprite;
        if (sprite != null)
        {
            // los iconos tienen PPU dispares y puede haber padres escalados:
            // escala local uniforme para un mismo tamaño mundial
            float parentScale = transform.parent != null
                ? Mathf.Max(transform.parent.lossyScale.x, 0.0001f)
                : 1f;
            float s = _worldSize / Mathf.Max(sprite.bounds.size.x, 0.001f) / parentScale;
            transform.localScale = new Vector3(s, s, s);
        }
        gameObject.SetActive(sprite != null);
    }

    void LateUpdate()
    {
        // posición en mundo: clavada al objeto seguido pero SIEMPRE hacia arriba
        if (_follow != null)
            transform.position = _follow.position
                + Vector3.up * (_height + Mathf.Sin(Time.time * BobSpeed) * BobAmplitude);

        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
        if (_cam == null) return;

        Vector3 toCam = _cam.position - transform.position;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }
}
