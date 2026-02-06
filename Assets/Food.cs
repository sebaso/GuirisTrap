using UnityEngine;

public class Food : MonoBehaviour
{
    [Header("Food Properties")]
    public string foodName = "Food";
    public float price = 10f;

    private Rigidbody rb;
    private Collider foodCollider;
    private bool isBeingHeld = false;

    public bool IsBeingHeld => isBeingHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        foodCollider = GetComponent<Collider>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        if (foodCollider == null)
        {
            foodCollider = gameObject.AddComponent<BoxCollider>();
        }
    }

    public void PickUp(Transform holdPoint)
    {
        isBeingHeld = true;

        rb.isKinematic = true;
        foodCollider.enabled = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        isBeingHeld = false;

        transform.SetParent(null);
        rb.isKinematic = false;
        foodCollider.enabled = true;
    }

    public void PlaceOnTable(Transform tablePoint)
    {
        isBeingHeld = false;

        transform.SetParent(tablePoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        rb.isKinematic = true;
        foodCollider.enabled = true;
    }

    public void Serve()
    {
        Debug.Log($"{foodName} has been served!");
        Destroy(gameObject);
    }
}
