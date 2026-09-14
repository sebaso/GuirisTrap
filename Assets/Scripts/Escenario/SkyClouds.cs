using System.Collections.Generic;
using UnityEngine;


public class SkyClouds : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] _sprites;

    [Header("Punto de referencia")]
    [SerializeField] private Transform _referencia;

    [Header("Cuántas y dónde")]
    [SerializeField] private int _cantidad = 10;
    [Tooltip("Altura sobre la cámara.")]
    [SerializeField] private float _altura = 45f;
    [SerializeField] private float _anchoFranja = 180f;
    [SerializeField] private float _dispersionAltura = 12f;

    [Header("Movimiento")]
    [SerializeField] private float _velocidadMin = 2f;
    [SerializeField] private float _velocidadMax = 6f;
    [SerializeField] private float _direccionViento = 0f;

    [Header("Aspecto")]
    [SerializeField] private float _tamanoMin = 7f;
    [SerializeField] private float _tamanoMax = 14f;
    [Range(0f, 1f)]
    [SerializeField] private float _opacidad = 0.75f;
    [SerializeField] private Light _sunLight;

    private Camera _cam;
    private readonly List<Nube> _nubes = new();

    private class Nube
    {
        public Transform tr;
        public SpriteRenderer sr;
        public float velocidad;
        public float offsetX;        
        public float offsetLateral;  
        public float altura;
        public float tamano;
    }

    void Awake()
    {
        _cam = Camera.main;
        if (_sunLight == null)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != null && l.type == LightType.Directional) { _sunLight = l; break; }
        }

        Sprite fallback = null;
        for (int i = 0; i < _cantidad; i++)
        {
            Sprite s = (_sprites != null && _sprites.Length > 0)
                ? _sprites[Random.Range(0, _sprites.Length)]
                : (fallback ??= GenerarNube());

            _nubes.Add(Crear(s, i));
        }
    }

    void LateUpdate()
    {
        if (_referencia == null)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return; // sin referencia ni cámara no hay dónde ponerlas
        }

        Color tinta = ColorPorLuz();

        float rad = _direccionViento * Mathf.Deg2Rad;
        Vector3 viento = new(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        Vector3 lateral = Vector3.Cross(Vector3.up, viento).normalized;

        foreach (Nube n in _nubes)
        {
            n.offsetX += n.velocidad * Time.deltaTime;

            if (n.offsetX > _anchoFranja * 0.5f) n.offsetX -= _anchoFranja;

            Vector3 centro = _referencia != null ? _referencia.position : _cam.transform.position;
            centro.y += n.altura;

            n.tr.position = centro + viento * n.offsetX + lateral * n.offsetLateral;

            n.tr.localScale = Vector3.one * n.tamano;

            n.sr.color = tinta;
        }
    }



    private Color ColorPorLuz()
    {
        Color c = Color.white;

        if (_sunLight != null)
        {

            float altura = Mathf.Clamp01(-_sunLight.transform.forward.y);
            c = Color.Lerp(_sunLight.color * 0.45f, Color.white, Mathf.Clamp01(altura * 1.4f));
        }

        c.a = _opacidad;
        return c;
    }

    private Nube Crear(Sprite sprite, int i)
    {
        GameObject go = new($"Nube_{i}");
        go.transform.SetParent(transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;
        sr.sortingOrder = -900; // delante del cielo, detrás de todo lo demás

        go.AddComponent<BillboardToCamera>();

        return new Nube
        {
            tr = go.transform,
            sr = sr,
            velocidad = Random.Range(_velocidadMin, _velocidadMax),
            offsetX = Random.Range(-_anchoFranja * 0.5f, _anchoFranja * 0.5f),
            offsetLateral = Random.Range(-_anchoFranja * 0.4f, _anchoFranja * 0.4f),
            altura = _altura + Random.Range(-_dispersionAltura, _dispersionAltura),
            tamano = Random.Range(_tamanoMin, _tamanoMax),
        };
    }


    private Sprite GenerarNube()
    {
        const int W = 256, H = 128;
        const float Base = 0.30f;

        // (centro, altura, anchura) de cada campana del perfil.
        (float cx, float amp, float w)[] bultos =
        {
            (0.24f, 0.16f, 0.10f),
            (0.40f, 0.30f, 0.14f),
            (0.56f, 0.26f, 0.13f),
            (0.72f, 0.18f, 0.10f),
            (0.86f, 0.10f, 0.07f),
        };

        Texture2D tex = new(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[W * H];

        for (int x = 0; x < W; x++)
        {
            float u = x / (float)W;

            float alto = 0f;
            foreach (var b in bultos)
            {
                float d = (u - b.cx) / b.w;
                alto += b.amp * Mathf.Exp(-d * d);
            }

            // Los extremos se recortan para que no acaben en pared vertical.
            float lados = Mathf.Clamp01((u - 0.08f) / 0.10f) * Mathf.Clamp01((0.94f - u) / 0.10f);
            float top = Base + alto * lados;

            for (int y = 0; y < H; y++)
            {
                float v = y / (float)H;
                if (v < Base - 0.02f || v > top) continue;

                float a = Mathf.Min(Mathf.Min((top - v) / 0.02f, (v - (Base - 0.02f)) / 0.02f), 1f);
                px[y * W + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
    }
}