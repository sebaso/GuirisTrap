using System.Collections.Generic;
using UnityEngine;


public class SandFootprints : MonoBehaviour
{
    [Header("Dónde se marcan")]
    [SerializeField] private Material[] _materialesDeArena;
    [SerializeField] private string _nombreMaterialContiene = "Arena";

    [SerializeField] private LayerMask _capasDeSuelo = ~0;

    [Header("Zancada")]
    [Tooltip("Cada cuántos metros recorridos se marca una huella.")]
    [SerializeField] private float _distanciaEntrePisadas = 0.55f;
    [Tooltip("Separación entre el pie izquierdo y el derecho.")]
    [SerializeField] private float _anchoEntrePies = 0.16f;
    [SerializeField] private float _alturaSobreSuelo = 0.02f;

    [Header("Aspecto")]
    [SerializeField] private Vector2 _tamano = new(0.22f, 0.36f);
    [SerializeField] private Color _color = new(0.62f, 0.51f, 0.36f, 0.85f);
    [SerializeField] private Texture2D _textura;

    [Header("Duración")]
    [SerializeField] private float _duracion = 4f;
    [Range(0.1f, 1f)]
    [SerializeField] private float _fraccionDesvanecido = 0.5f;
    [SerializeField] private int _maxPisadas = 30;

    [Header("Sonido")]
    [SerializeField] private string _sfxPisada = "";

    private readonly List<Footprint> _pool = new();
    private int _next;
    private Vector3 _ultimaPos;
    private float _distanciaAcumulada;
    private bool _pieDerecho;
    private Mesh _quad;
    private Material _mat;

    private class Footprint
    {
        public Transform tr;
        public MeshRenderer mr;
        public MaterialPropertyBlock mpb;
        public float nacida;
        public bool viva;
    }

    void Awake()
    {
        _ultimaPos = transform.position;
        _quad = BuildQuad();
        _mat = BuildMaterial();
    }

    void Update()
    {
        AcumularDistancia();
        Desvanecer();
    }


    private void AcumularDistancia()
    {
        Vector3 pos = transform.position;
        Vector3 delta = pos - _ultimaPos;
        delta.y = 0f; // saltar o bajar no cuenta como andar
        _ultimaPos = pos;

        _distanciaAcumulada += delta.magnitude;
        if (_distanciaAcumulada < _distanciaEntrePisadas) return;

        _distanciaAcumulada = 0f;
        IntentarPisada(delta);
    }

    private void IntentarPisada(Vector3 direccion)
    {
        Vector3 origen = transform.position + Vector3.up * 0.5f;

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 3f,
                             _capasDeSuelo, QueryTriggerInteraction.Ignore))
            return;

        if (!EsArena(hit.collider)) return;


        Vector3 fwd = direccion.sqrMagnitude > 0.0001f ? direccion.normalized : transform.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, fwd).normalized;

        _pieDerecho = !_pieDerecho;
        float lado = _pieDerecho ? 1f : -1f;

        Vector3 punto = hit.point
                      + hit.normal * _alturaSobreSuelo
                      + derecha * (_anchoEntrePies * 0.5f * lado);

        Colocar(punto, fwd, hit.normal, lado);

        if (!string.IsNullOrEmpty(_sfxPisada))
            AudioManager.Instance?.PlaySFX(_sfxPisada);
    }

    /// <summary>¿El jugador está ahora mismo pisando arena? Lo usan las flechas
    /// guía para esconderse en la playa. Se expone desde aquí para que la lista
    /// de materiales de arena se configure en UN solo sitio.</summary>
    public bool JugadorSobreArena()
    {
        Vector3 origen = transform.position + Vector3.up * 0.5f;

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 3f,
                             _capasDeSuelo, QueryTriggerInteraction.Ignore))
            return false;

        return EsArena(hit.collider);
    }

    [ContextMenu("DEBUG: qué estoy pisando")]
    public void DebugQuePiso()
    {
        Vector3 origen = transform.position + Vector3.up * 0.5f;

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 3f,
                             _capasDeSuelo, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning($"[SandFootprints] El rayo no encuentra NADA bajo {name}. " +
                             "Revisa Capas De Suelo y que el suelo tenga collider.", this);
            return;
        }

        Renderer rend = hit.collider.GetComponent<Renderer>()
                     ?? hit.collider.GetComponentInParent<Renderer>()
                     ?? hit.collider.GetComponentInChildren<Renderer>();

        string mats = "(sin Renderer)";
        if (rend != null)
        {
            var nombres = new System.Text.StringBuilder();
            foreach (Material m in rend.sharedMaterials)
                nombres.Append(m != null ? m.name : "null").Append("  ");
            mats = nombres.ToString();
        }

        Debug.Log($"[SandFootprints] Piso: '{hit.collider.name}'\n" +
                  $"  Materiales: {mats}\n" +
                  $"  ¿Cuenta como arena?: {EsArena(hit.collider)}\n" +
                  $"  Buscando por nombre: '{_nombreMaterialContiene}'", hit.collider);
    }

    public bool EsArena(Collider col)
    {
        if (col == null) return false;

        Renderer rend = col.GetComponent<Renderer>();
        if (rend == null) rend = col.GetComponentInParent<Renderer>();
        if (rend == null) rend = col.GetComponentInChildren<Renderer>();
        if (rend == null) return false;

        foreach (Material m in rend.sharedMaterials)
        {
            if (m == null) continue;

            if (_materialesDeArena != null)
            {
                foreach (Material arena in _materialesDeArena)
                    if (arena != null && m == arena) return true;
            }

            if (!string.IsNullOrEmpty(_nombreMaterialContiene) &&
                m.name.IndexOf(_nombreMaterialContiene, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void Colocar(Vector3 pos, Vector3 fwd, Vector3 normal, float lado)
    {
        Footprint f = GetFootprint();

        f.tr.position = pos;
        // Tumbada sobre el suelo, siguiendo su inclinación y mirando al frente.
        f.tr.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(fwd, normal).normalized, normal) * Quaternion.Euler(90f, 0f, 0f);

        f.tr.localScale = new Vector3(_tamano.x * lado, _tamano.y, 1f);

        f.nacida = Time.time;
        f.viva = true;
        f.tr.gameObject.SetActive(true);
        AplicarAlpha(f, 1f);
    }

    private void Desvanecer()
    {
        foreach (Footprint f in _pool)
        {
            if (!f.viva) continue;

            float t = (Time.time - f.nacida) / Mathf.Max(0.01f, _duracion);

            if (t >= 1f)
            {
                f.viva = false;
                f.tr.gameObject.SetActive(false);
                continue;
            }

            // Opaca hasta que entra en el tramo final, y ahí se va apagando.
            float inicioFade = 1f - _fraccionDesvanecido;
            float alpha = t < inicioFade
                ? 1f
                : 1f - Mathf.InverseLerp(inicioFade, 1f, t);

            AplicarAlpha(f, alpha);
        }
    }

    private void AplicarAlpha(Footprint f, float a)
    {
        Color c = _color;
        c.a *= a;

        f.mpb ??= new MaterialPropertyBlock();
        f.mr.GetPropertyBlock(f.mpb);
        f.mpb.SetColor("_Color", c);
        f.mpb.SetColor("_BaseColor", c); // URP usa este nombre
        f.mr.SetPropertyBlock(f.mpb);
    }


    private Footprint GetFootprint()
    {
        foreach (Footprint f in _pool)
            if (!f.viva) return f;

        if (_pool.Count < _maxPisadas) return Crear();

        Footprint viejo = _pool[_next];
        _next = (_next + 1) % _pool.Count;
        return viejo;
    }

    private Footprint Crear()
    {
        GameObject go = new($"Pisada_{_pool.Count}");
        go.transform.SetParent(null); // se quedan en el mundo, no siguen al jugador

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = _quad;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        go.SetActive(false);

        Footprint f = new() { tr = go.transform, mr = mr };
        _pool.Add(f);
        return f;
    }

    private Mesh BuildQuad()
    {
        Mesh m = new() { name = "Pisada" };
        m.vertices = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0),
                              new Vector3(-0.5f, 0.5f,0), new Vector3(0.5f, 0.5f,0) };
        m.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        m.normals = new[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
        return m;
    }

    private Material BuildMaterial()
    {
        Shader sh = Shader.Find("Sprites/Default");
        Material mat = new(sh)
        {
            mainTexture = _textura != null ? _textura : GenerarTextura(),
            renderQueue = 3000, 
        };
        return mat;
    }

    private Texture2D GenerarTextura()
    {
        const int S = 128;
        Texture2D tex = new(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        Color32[] px = new Color32[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 0);

        PintarOvalo(px, S, 0.50f, 0.28f, 0.20f, 0.16f, 1f); 
        PintarOvalo(px, S, 0.50f, 0.58f, 0.24f, 0.18f, 1f); 
        PintarOvalo(px, S, 0.50f, 0.43f, 0.13f, 0.12f, 1f); 


        PintarOvalo(px, S, 0.38f, 0.79f, 0.075f, 0.065f, 1f);
        PintarOvalo(px, S, 0.49f, 0.83f, 0.060f, 0.055f, 1f);
        PintarOvalo(px, S, 0.58f, 0.82f, 0.052f, 0.048f, 1f);
        PintarOvalo(px, S, 0.65f, 0.79f, 0.045f, 0.042f, 1f);
        PintarOvalo(px, S, 0.71f, 0.75f, 0.040f, 0.038f, 1f);

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    private void PintarOvalo(Color32[] px, int size, float cx, float cy, float rx, float ry, float a)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt((cx - rx) * size));
        int x1 = Mathf.Min(size - 1, Mathf.CeilToInt((cx + rx) * size));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((cy - ry) * size));
        int y1 = Mathf.Min(size - 1, Mathf.CeilToInt((cy + ry) * size));

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x / (float)size - cx) / rx;
                float dy = (y / (float)size - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d > 1f) continue;

                byte alpha = (byte)(Mathf.Clamp01((1f - d) * 2.2f) * 255 * a);
                int i = y * size + x;
                if (alpha > px[i].a) px[i] = new Color32(255, 255, 255, alpha);
            }
        }
    }
}

