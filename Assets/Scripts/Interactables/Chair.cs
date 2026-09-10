using UnityEngine;

[RequireComponent(typeof(PlaceableObject))]
public class Chair : MonoBehaviour
{
    [Header("Seat Point")]
    [Tooltip("The exact transform where a client will sit. If null, the chair's own transform is used.")]
    public Transform seatPoint;
    public bool IsPlaced { get; private set; }
    public Transform SeatTransform => seatPoint != null ? seatPoint : transform;

    public Client Occupant { get; set; }
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
        IsPlaced = true;
    }

    // while carried, don't count as a seat
    public void SetCarried(bool carried)
    {
        IsPlaced = !carried;
        if (carried && Occupant != null)
        {
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