using UnityEngine;


[RequireComponent(typeof(PlayerController))]
public class RecipeGuideArrow : MonoBehaviour
{
    [Header("Colocación")]
    [SerializeField] private float _height = 2.6f;
    [SerializeField] private float _size = 0.9f;

    [Header("Orientación")]
    [SerializeField] private bool _billboardToCamera = true;
    [SerializeField] private float _tiltDegrees = 25f;

    [Header("Animación")]
    [SerializeField] private float _bobAmplitude = 0.12f;
    [SerializeField] private float _bobSpeed     = 3f;
    [SerializeField] private float _pulseAmount  = 0.08f;
    [SerializeField] private float _pulseSpeed   = 4f;

    [Header("Comportamiento")]
    [SerializeField] private float _hideWithinDistance = 2.5f;

    [Header("Aspecto")]
    [SerializeField] private Color _arrowColor = new Color(1f, 0.55f, 0.1f, 1f);
    [SerializeField] private string _shaderName = "Guiri/GuideArrow";

    private PlayerController _player;
    private Camera    _cam;
    private Transform _arrow;
    private Material  _mat;

    private RecipeData       _lastRecipe;
    private CookingStation[] _matchingStations = System.Array.Empty<CookingStation>();
    private Transform        _espeteraFallback;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _cam    = Camera.main;
        BuildArrow();
        _arrow.gameObject.SetActive(false);
    }

    void LateUpdate() // después de que el jugador (y la cámara) se hayan movido
    {
        if (_cam == null) _cam = Camera.main; // por si la cámara aparece tarde

        RecipeData recipe = _player != null ? _player.currentRecipe : null;

        // ¿Ha cambiado lo que llevamos? → recalcular estaciones objetivo.
        if (recipe != _lastRecipe)
        {
            _lastRecipe = recipe;
            Retarget(recipe);
        }

        Transform target = recipe != null ? NearestTarget() : null;
        bool show = target != null;

        if (show)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            // Cerca de la estación la flecha sobra (el sonar de la estación ya guía).
            if (toTarget.sqrMagnitude <= _hideWithinDistance * _hideWithinDistance
             || toTarget.sqrMagnitude < 0.0001f)
            {
                show = false;
            }
            else
            {
                float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;
                _arrow.position = transform.position + Vector3.up * (_height + bob);

                OrientArrow(target);

                float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
                _arrow.localScale = Vector3.one * (_size * pulse);
            }
        }

        if (_arrow.gameObject.activeSelf != show)
            _arrow.gameObject.SetActive(show);
    }

    private void OrientArrow(Transform target)
    {
        if (_billboardToCamera && _cam != null)
        {
            // Dirección hacia el objetivo EN PANTALLA.
            Vector3 selfScreen = _cam.WorldToScreenPoint(_arrow.position);
            Vector3 tgtScreen  = _cam.WorldToScreenPoint(target.position);
            Vector2 screenDir  = (Vector2)(tgtScreen - selfScreen);

            if (screenDir.sqrMagnitude < 1f) return; // objetivo encima: mantener rotación

            // Llevar esa dirección 2D al plano de la cámara en el mundo.
            Vector3 worldScreenDir = (_cam.transform.right * screenDir.x +
                                      _cam.transform.up    * screenDir.y).normalized;

            // Malla: apunta a +Z y su normal es +Y → +Z hacia el objetivo en
            // el plano de pantalla, +Y (la cara) hacia la cámara.
            _arrow.rotation = Quaternion.LookRotation(worldScreenDir, -_cam.transform.forward);
        }
        else
        {
            // Modo v1: flecha plana en el mundo con inclinación.
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            _arrow.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up)
                            * Quaternion.Euler(_tiltDegrees, 0f, 0f);
        }
    }

    private void Retarget(RecipeData recipe)
    {
        _matchingStations = System.Array.Empty<CookingStation>();
        _espeteraFallback = null;

        if (recipe == null) return;

        // Todas las estaciones cuyo tipo encaja con la receta.
        CookingStation[] all = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
        var matches = new System.Collections.Generic.List<CookingStation>();
        foreach (CookingStation s in all)
            if (s.stationType == recipe.type) matches.Add(s);
        _matchingStations = matches.ToArray();

        // Recetas de espeto: no van a una CookingStation, van a la espetera.
        if (_matchingStations.Length == 0 &&
            recipe.type.ToString().ToLowerInvariant().Contains("espet"))
        {
            EspetoMinigame espetera = FindFirstObjectByType<EspetoMinigame>();
            if (espetera != null) _espeteraFallback = espetera.transform;
        }
    }

    /// <summary>La estación válida más cercana al jugador en este momento.</summary>
    private Transform NearestTarget()
    {
        Transform best = _espeteraFallback;
        float bestDist = best != null
            ? (best.position - transform.position).sqrMagnitude
            : float.MaxValue;

        foreach (CookingStation s in _matchingStations)
        {
            if (s == null) continue;
            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = s.transform; }
        }

        return best;
    }

    // ------------------------------------------------------------------
    //  Construcción de la flecha (malla procedural, sin arte)
    // ------------------------------------------------------------------

    private void BuildArrow()
    {
        GameObject go = new GameObject("RecipeGuideArrow");
        go.transform.SetParent(transform, false); // hija del player (se mueve con él)
        _arrow = go.transform;

        // Flecha plana apuntando a +Z, normal +Y. uv.y: 0 = cola, 1 = punta
        // (para el flujo del shader).
        Mesh mesh = new Mesh { name = "GuideArrow (procedural)" };

        Vector3[] verts =
        {
            new Vector3( 0.00f, 0f,  0.50f), // 0: punta
            new Vector3(-0.28f, 0f,  0.08f), // 1: ala izquierda
            new Vector3( 0.28f, 0f,  0.08f), // 2: ala derecha
            new Vector3(-0.11f, 0f,  0.08f), // 3: hombro izq. del mástil
            new Vector3( 0.11f, 0f,  0.08f), // 4: hombro der. del mástil
            new Vector3(-0.11f, 0f, -0.45f), // 5: cola izq.
            new Vector3( 0.11f, 0f, -0.45f), // 6: cola der.
        };

        Vector2[] uvs =
        {
            new Vector2(0.5f,  1.00f),
            new Vector2(0.0f,  0.56f),
            new Vector2(1.0f,  0.56f),
            new Vector2(0.3f,  0.56f),
            new Vector2(0.7f,  0.56f),
            new Vector2(0.3f,  0.00f),
            new Vector2(0.7f,  0.00f),
        };

        int[] tris =
        {
            0, 2, 1,   // cabeza
            3, 4, 6,   // mástil
            3, 6, 5,
        };

        Vector3[] normals = new Vector3[verts.Length];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;

        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.normals   = normals;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        Shader shader = Shader.Find(_shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[RecipeGuideArrow] No se encontró el shader '{_shaderName}'. " +
                             "Usando Sprites/Default de repuesto (funcional pero menos chulo).");
            shader = Shader.Find("Sprites/Default");
        }

        _mat = new Material(shader);
        if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", _arrowColor);
        mr.material = _mat;
    }

    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}