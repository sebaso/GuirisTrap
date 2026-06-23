using UnityEngine;

[RequireComponent(typeof(CookingStation))]
public class StationIndicatorController : MonoBehaviour
{
    [Header("Indicador")]
    public GameObject indicatorQuad;

    [Header("Auto-creación")]
    public string shaderName = "Guiri/StationIndicator";
    public float autoHeight = 2f;
    public float autoSize = 1.5f;
    public bool autoFlat = true;

    [Header("Color por tipo")]
    public Color indicatorColor = new Color(1f, 0.1f, 0.5f, 1f);

    private CookingStation _station;
    private PlayerController _player;
    private Material _mat;
    private bool _isShowing = false;

    void Awake()
    {
        _station = GetComponent<CookingStation>();

        // Si no hay quad asignado, lo creamos por código.
        if (indicatorQuad == null)
        {
            indicatorQuad = CreateIndicatorQuad();
        }

        if (indicatorQuad != null)
        {
            Renderer rend = indicatorQuad.GetComponent<Renderer>();
            if (rend != null)
            {
                // Instancia propia del material para no afectar a otras estaciones
                _mat = new Material(rend.sharedMaterial);
                _mat.SetColor("_Color", indicatorColor);
                rend.material = _mat;
            }
            indicatorQuad.SetActive(false);
        }
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) _player = playerObj.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (_player == null || indicatorQuad == null) return;

        bool shouldShow = _player.currentRecipe != null
                       && _player.currentRecipe.type == _station.stationType;

        if (shouldShow != _isShowing)
        {
            _isShowing = shouldShow;
            indicatorQuad.SetActive(_isShowing);
        }
    }


    private GameObject CreateIndicatorQuad()
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Indicator (auto)";
        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = new Vector3(0f, autoHeight, 0f);
        quad.transform.localScale    = Vector3.one * autoSize;

        // Orientación: tumbado (mirando hacia arriba) o vertical.
        if (autoFlat)
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        else
            quad.transform.localRotation = Quaternion.identity;


        Collider col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);


        Shader shader = Shader.Find(shaderName);
        Renderer rend = quad.GetComponent<Renderer>();
        if (shader != null && rend != null)
        {
            rend.sharedMaterial = new Material(shader);
        }
        else if (shader == null)
        {
            Debug.LogWarning($"[StationIndicator] No se encontró el shader '{shaderName}'. " +
                             "La has liado parda. Esto es inadmisible.");
        }

        return quad;
    }
}