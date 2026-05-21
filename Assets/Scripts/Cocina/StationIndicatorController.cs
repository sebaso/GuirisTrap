using UnityEngine;

[RequireComponent(typeof(CookingStation))]
public class StationIndicatorController : MonoBehaviour
{
    [Header("Indicador")]
    public GameObject indicatorQuad;

    [Header("Color por tipo")]
    public Color indicatorColor = new Color(1f, 0.1f, 0.5f, 1f);

    private CookingStation _station;
    private PlayerController _player;
    private Material _mat;
    private bool _isShowing = false;

    void Awake()
    {
        _station = GetComponent<CookingStation>();

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
}
