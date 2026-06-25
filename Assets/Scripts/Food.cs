using UnityEngine;

public class Food : MonoBehaviour
{
    [Header("Food Properties")]
    public string foodName = "Food";
    public float price = 10f;

    private Rigidbody rb;
    // The pickup logic finds Food via GetComponentInParent, so the collider(s)
    // may live on children of the prefab root. Toggle all of them together,
    // otherwise a child collider stays active while held and keeps registering
    // physics overlaps. Cached including inactive ones.
    private Collider[] colliders;
    private bool isBeingHeld = false;
    private bool isServed = false;

    public bool IsBeingHeld => isBeingHeld;
    public bool IsServed => isServed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        if (colliders.Length == 0)
        {
            colliders = new Collider[] { gameObject.AddComponent<BoxCollider>() };
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider c in colliders)
            if (c != null) c.enabled = enabled;
    }

    public void PickUp(Transform holdPoint)
    {
        isBeingHeld = true;
        rb.isKinematic = true;
        SetCollidersEnabled(false);
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        isBeingHeld = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        SetCollidersEnabled(true);
    }

    public void PlaceOnTable(Transform tablePoint)
    {
        isBeingHeld = false;
        isServed = true;

        transform.SetParent(tablePoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        rb.isKinematic = true;
        SetCollidersEnabled(false);
    }

    public void Serve()
    {
        Debug.Log($"{foodName} has been served!");
        Destroy(gameObject);
    }
}
