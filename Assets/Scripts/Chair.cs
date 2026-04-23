using UnityEngine;

[RequireComponent(typeof(PlaceableObject))]
public class Chair : MonoBehaviour
{
    [Header("Seat Point")]
    [Tooltip("The exact transform where a client will sit. If null, the chair's own transform is used.")]
    public Transform seatPoint;
    public bool IsPlaced { get; private set; }
    public Transform SeatTransform => seatPoint != null ? seatPoint : transform;

    private PlaceableObject _placeable;
    private bool _wasStoraged;

    void Awake()
    {
        _placeable = GetComponent<PlaceableObject>();

        if (seatPoint == null)
            seatPoint = transform;
    }

    void Start()
    {
        if (_placeable == null || !_placeable.Storaged)
        {
            IsPlaced = true;
            _wasStoraged = false;
        }
        else
        {
            IsPlaced = false;
            _wasStoraged = true;
        }
    }

    void Update()
    {
        if (_placeable == null) return;

        bool isCurrentlyStored = _placeable.Storaged;

        if (isCurrentlyStored && !_wasStoraged)
        {
            IsPlaced = false;
            _wasStoraged = true;
        }
        else if (!isCurrentlyStored && _wasStoraged)
        {
            IsPlaced = true;
            _wasStoraged = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (IsPlaced)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        if (seatPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(seatPoint.position, 0.2f);
        }
    }
}
