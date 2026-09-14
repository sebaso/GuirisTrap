using UnityEngine;


[DisallowMultipleComponent]
public class SkyAndCelestials : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Light _sunLight;
    [SerializeField] private Material _skyMaterial;

    [Header("Colores del cielo a lo largo del día")]
    [SerializeField] private Gradient _zenitPorAltura = DefaultZenit();
    [SerializeField] private Gradient _horizontePorAltura = DefaultHorizonte();

    [Header("Punto de referencia")]
    [SerializeField] private Transform _referencia;

    [Header("Astros")]
    [SerializeField] private float _distancia = 150f;
    [SerializeField] private float _tamanoSol = 22f;
    [SerializeField] private float _tamanoLuna = 16f;
    [SerializeField] private Sprite _spriteSol;
    [SerializeField] private Sprite _spriteLuna;
    [SerializeField] private Color _tintaSol = new(1f, 0.93f, 0.62f, 1f);
    [SerializeField] private Color _tintaLuna = new(0.92f, 0.94f, 1f, 1f);

    [Header("Desvanecido bajo el horizonte")]
    [SerializeField] private float _alturaDesvanecido = 0.08f;

    [Range(0f, 0.5f)]
    [SerializeField] private float _adelantoLuna = 0.2f;

    private Camera _cam;
    private Transform _sol, _luna;
    private SpriteRenderer _srSol, _srLuna;

    void Awake()
    {
        _cam = Camera.main;
        if (_sunLight == null) _sunLight = FindMainDirectional();

        _sol  = CrearAstro("Sol",  _spriteSol  != null ? _spriteSol  : GenerarSol(),  _tintaSol,  out _srSol);
        _luna = CrearAstro("Luna", _spriteLuna != null ? _spriteLuna : GenerarLuna(), _tintaLuna, out _srLuna);
    }

    void Start()
    {
        if (_cam != null && _distancia >= _cam.farClipPlane)
        {
            Debug.LogWarning($"[SkyAndCelestials] Distancia ({_distancia}) mayor que el Far Clip " +
                             $"de la cámara ({_cam.farClipPlane}): el sol y la luna no se verán. " +
                             "Baja la distancia o sube el Far Clip.", this);
        }
    }

    void LateUpdate()
    {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
        if (_sunLight == null) { _sunLight = FindMainDirectional(); if (_sunLight == null) return; }

        Vector3 haciaSol = -_sunLight.transform.forward;
        Vector3 haciaLuna = -haciaSol;             

        Colocar(_sol,  _srSol,  haciaSol,  _tamanoSol,  _tintaSol,  0f);
        Colocar(_luna, _srLuna, haciaLuna, _tamanoLuna, _tintaLuna, _adelantoLuna);

        ActualizarCielo(haciaSol.y);
    }

    // ------------------------------------------------------------------

    private void Colocar(Transform astro, SpriteRenderer sr, Vector3 dir, float tamano, Color tinta, float adelanto)
    {
        if (astro == null) return;

        Vector3 origen = _referencia != null ? _referencia.position : _cam.transform.position;
        astro.position = origen + dir * _distancia;
        astro.localScale = Vector3.one * tamano;


        float visible = Mathf.InverseLerp(-_alturaDesvanecido - adelanto, _alturaDesvanecido, dir.y);
        Color c = tinta;
        c.a *= Mathf.Clamp01(visible);

        if (sr != null)
        {
            sr.color = c;
            bool activo = c.a > 0.01f;
            if (astro.gameObject.activeSelf != activo) astro.gameObject.SetActive(activo);
        }
    }

    private void ActualizarCielo(float alturaSol)
    {
        if (_skyMaterial == null) return;

        float t = Mathf.Clamp01(alturaSol);

        _skyMaterial.SetColor("_ZenithColor",  _zenitPorAltura.Evaluate(t));
        _skyMaterial.SetColor("_HorizonColor", _horizontePorAltura.Evaluate(t));
    }


    private Transform CrearAstro(string nombre, Sprite sprite, Color tinta, out SpriteRenderer sr)
    {
        GameObject go = new(nombre);
        go.transform.SetParent(transform, false);

        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tinta;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;

        sr.sortingOrder = -1000;

        go.AddComponent<BillboardToCamera>();

        return go.transform;
    }

    private Light FindMainDirectional()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l != null && l.type == LightType.Directional) return l;
        return null;
    }

    private Sprite GenerarSol()
    {
        Texture2D t = Disco(128, 0.42f, 0.16f, true);
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GenerarLuna()
    {
        Texture2D t = Disco(128, 0.40f, 0.08f, false);

        Color32[] px = t.GetPixels32();
        const int S = 128;
        float cx = 0.62f, cy = 0.54f, r = 0.34f;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = x / (float)S - cx, dy = y / (float)S - cy;
                if (dx * dx + dy * dy < r * r) px[y * S + x] = new Color32(255, 255, 255, 0);
            }
        }

        t.SetPixels32(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Texture2D Disco(int size, float radio, float halo, bool conHalo)
    {
        Texture2D tex = new(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x / (float)size - 0.5f;
                float dy = y / (float)size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                byte a;
                if (d <= radio)
                {
                    a = 255;
                }
                else if (conHalo && d <= radio + halo)
                {
                    a = (byte)(Mathf.Clamp01(1f - (d - radio) / halo) * 110);
                }
                else
                {
                    a = 0;
                }

                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // ---- Gradientes por defecto ----

    private static Gradient DefaultZenit()
    {
        Gradient g = new();
        g.SetKeys(new[]
        {
            new GradientColorKey(new Color(0.06f, 0.08f, 0.22f), 0.00f), // noche
            new GradientColorKey(new Color(0.30f, 0.38f, 0.62f), 0.15f), // amanecer
            new GradientColorKey(new Color(0.26f, 0.58f, 0.92f), 0.55f), // día
            new GradientColorKey(new Color(0.22f, 0.62f, 0.98f), 1.00f), // mediodía
        }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    private static Gradient DefaultHorizonte()
    {
        Gradient g = new();
        g.SetKeys(new[]
        {
            new GradientColorKey(new Color(0.12f, 0.12f, 0.26f), 0.00f),
            new GradientColorKey(new Color(0.98f, 0.62f, 0.42f), 0.12f), // naranja del amanecer
            new GradientColorKey(new Color(0.99f, 0.88f, 0.70f), 0.45f),
            new GradientColorKey(new Color(0.85f, 0.94f, 1.00f), 1.00f),
        }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}