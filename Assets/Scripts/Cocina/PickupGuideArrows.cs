using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PickupGuideArrows : MonoBehaviour
{
    [Header("Colocación")]
    [SerializeField] private float _height = 2.2f;
    [SerializeField] private float _size = 0.7f;
    [SerializeField] private float _ringRadius = 0.55f;

    [Header("Animación")]
    [SerializeField] private float _bobAmplitude = 0.1f;
    [SerializeField] private float _bobSpeed = 3f;

    [Header("Comportamiento")]
    [SerializeField] private float _hideWithinDistance = 3f;
    [SerializeField] private float _refreshInterval = 0.4f;
    [SerializeField] private int _maxArrows = 4;

    [Header("Aspecto")]
    [SerializeField] private string _shaderName = "Guiri/GuideArrow";
    [Range(0.2f, 1f)]
    [SerializeField] private float _alpha = 0.85f;

    private PlayerController _player;
    private Camera _cam;
    private float _nextRefresh;

    private readonly List<Transform> _pool = new();
    private readonly List<Material> _mats = new();
    private readonly List<(Transform target, Color color)> _targets = new();

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;

        // Con un ingrediente en la mano manda la flecha de cocinar: estas se van.
        if (_player == null || _player.currentRecipe != null)
        {
            HideAll();
            return;
        }

        if (Time.time >= _nextRefresh)
        {
            _nextRefresh = Time.time + _refreshInterval;
            RecomputeTargets();
        }

        Draw();
    }

    // ------------------------------------------------------------------

    private void RecomputeTargets()
    {
        _targets.Clear();

        var wanted = OrderGuide.Wanted;
        if (wanted.Count == 0) return;

        foreach (FoodStorage storage in FindObjectsByType<FoodStorage>(FindObjectsSortMode.None))
        {
            if (storage == null || storage.recipes == null) continue;

            // ¿Este electrodoméstico tiene algo de lo que están pidiendo?
            RecipeData match = null;
            foreach (RecipeData r in storage.recipes)
            {
                if (r != null && OrderGuide.IsWanted(r)) { match = r; break; }
            }
            if (match == null) continue;

            Color c = Color.white;
            if (ColorUtility.TryParseHtmlString(RecipeStations.ColorHex(match.type), out Color parsed))
                c = parsed;
            c.a = _alpha;

            _targets.Add((storage.transform, c));
            if (_targets.Count >= _maxArrows) break;
        }
    }

    private void Draw()
    {
        int used = 0;

        for (int i = 0; i < _targets.Count; i++)
        {
            (Transform target, Color color) = _targets[i];
            if (target == null) continue;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            // Ya estás al lado: la flecha estorba más que ayuda.
            if (toTarget.sqrMagnitude <= _hideWithinDistance * _hideWithinDistance) continue;
            if (toTarget.sqrMagnitude < 0.0001f) continue;

            Transform arrow = GetArrow(used);
            SetColor(used, color);
            used++;

            Vector3 dir = toTarget.normalized;
            float bob = Mathf.Sin(Time.time * _bobSpeed + i * 1.3f) * _bobAmplitude;

            // Cada flecha se separa hacia SU objetivo, así que dos que apunten a
            // sitios distintos nunca acaban una encima de la otra.
            arrow.position = transform.position + Vector3.up * (_height + bob) + dir * _ringRadius;

            Orient(arrow, target);
            arrow.localScale = Vector3.one * _size;

            if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);
        }

        for (int i = used; i < _pool.Count; i++)
            if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false);
    }

    private void Orient(Transform arrow, Transform target)
    {
        if (_cam == null) return;

        Vector3 selfScreen = _cam.WorldToScreenPoint(arrow.position);
        Vector3 tgtScreen  = _cam.WorldToScreenPoint(target.position);
        Vector2 screenDir  = (Vector2)(tgtScreen - selfScreen);

        if (screenDir.sqrMagnitude < 1f) return;

        Vector3 worldScreenDir = (_cam.transform.right * screenDir.x +
                                  _cam.transform.up * screenDir.y).normalized;

        arrow.rotation = Quaternion.LookRotation(worldScreenDir, -_cam.transform.forward);
    }

    private void HideAll()
    {
        foreach (Transform t in _pool)
            if (t.gameObject.activeSelf) t.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------

    private Transform GetArrow(int index)
    {
        while (_pool.Count <= index) BuildArrow();
        return _pool[index];
    }

    private void SetColor(int index, Color c)
    {
        if (index < _mats.Count && _mats[index] != null && _mats[index].HasProperty("_Color"))
            _mats[index].SetColor("_Color", c);
    }

    private void BuildArrow()
    {
        GameObject go = new($"PickupGuideArrow_{_pool.Count}");
        go.transform.SetParent(transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GuideArrowMesh.Get();

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Shader shader = Shader.Find(_shaderName) ?? Shader.Find("Sprites/Default");
        Material mat = new(shader);
        mr.material = mat;

        go.SetActive(false);
        _pool.Add(go.transform);
        _mats.Add(mat);
    }
}