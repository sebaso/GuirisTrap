using UnityEngine;

[RequireComponent(typeof(PlaceableObject))]
public class Chair : MonoBehaviour
{
    [Header("Seat Point")]
    [Tooltip("The exact transform where a client will sit. If null, the chair's own transform is used.")]
    public Transform seatPoint;
    public bool IsPlaced { get; private set; }
    public Transform SeatTransform => seatPoint != null ? seatPoint : transform;

    // claimant from walk-up through eating; null = free
    public Client Occupant { get; set; }

    // Una silla con un Occupant asignado no se puede recoger,
    // aunque el cliente esté aún caminando hacia ella (WalkingToTable).
    public bool IsBeingSatOn => Occupant != null;

    private PlaceableObject _placeable;

    void Awake()
    {
        _placeable = GetComponent<PlaceableObject>();

        if (seatPoint == null)
            seatPoint = transform;
    }

    void Start()
    {
        if (_placeable == null)
        {
            IsPlaced = true;
        }
        else
        {
            IsPlaced = false;
        }
    }

    void Update()
    {
        if (_placeable == null) return;
    }

    // while carried, don't count as a seat
    public void SetCarried(bool carried)
    {
        IsPlaced = !carried;
        if (carried && Occupant != null)
        {
            // Forzar liberación solo si se logró agarrar (no debería ocurrir con IsBeingSatOn).
            Occupant = null;
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
