using UnityEngine;

public class GridZone : MonoBehaviour
{
    [Header("Identidad de la zona")]
    [SerializeField] 
    private ZoneId _zoneId;
    public ZoneId ZoneId => _zoneId;

    [Header("Componentes de esta zona")]
    [SerializeField] 
    private GridManager _gridManager;
    [SerializeField] 
    private PlaceableInstanceRegistry _registry;
    [SerializeField] 
    private MonoBehaviour _resolverBehaviour;
    private IGridWorldResolver _resolver;

    [Header("Solo en escenas de edición (ej. PreparationScene)")]
    [Tooltip("Opcional: solo necesario donde haya interacción de colocación con el ratón. En GameScene se deja vacío.")]
    [SerializeField] private GridProjectionVisibility _projectionVisibility;

    public GridManager GridManager => _gridManager;
    public PlaceableInstanceRegistry Registry => _registry;
    public IGridWorldResolver Resolver => _resolver;
    public GridProjectionVisibility ProjectionVisibility => _projectionVisibility;

    void Awake()
    {
        _resolver = _resolverBehaviour as IGridWorldResolver;
    }
}