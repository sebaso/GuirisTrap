using UnityEngine;

public class Food : MonoBehaviour
{
    [Header("Food Properties")]
    public string foodName = "Food";
    public float price = 10f;

    // The recipe that produced this plate. Set at cook time by
    // PlayerController.CreateAndHoldFood so orders can be matched by reference
    // (foodName strings are unreliable across prefabs). Null for legacy plates.
    public RecipeData recipe;

    private Rigidbody rb;
    // The pickup logic finds Food via GetComponentInParent, so the collider(s)
    // may live on children of the prefab root. Toggle all of them together,
    // otherwise a child collider stays active while held and keeps registering
    // physics overlaps. Cached including inactive ones.
    private Collider[] colliders;
    // Mientras el plato se lleva en la bandeja como icono 2D, su modelo 3D
    // duerme; se reactiva al soltarlo o servirlo.
    private Renderer[] _renderers;
    // Icono mostrado sobre la mesa una vez servido (muere con este GameObject).
    private FoodIcon _servedIcon;
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

        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    public void SetModelVisible(bool visible)
    {
        if (_renderers == null) return;
        foreach (Renderer r in _renderers)
            if (r != null) r.enabled = visible;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null) return;
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
        SetModelVisible(true);
    }

    public void PlaceOnTable(Transform tablePoint)
    {
        isBeingHeld = false;
        isServed = true;

        transform.SetParent(tablePoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // rb puede ser null si Awake no corrió (herramientas de editor)
        if (rb != null) rb.isKinematic = true;
        SetCollidersEnabled(false);

        // servido = icono 2D sobre la mesa (el modelo 3D es un placeholder);
        // sin icono disponible, se queda el modelo de siempre
        Sprite icon = recipe != null ? recipe.icon : null;
        if (icon == null)
        {
            SetModelVisible(true);
            return;
        }
        SetModelVisible(false);
        if (_servedIcon == null)
            _servedIcon = FoodIcon.Create(transform, 0.35f, 0.35f);
        _servedIcon.Set(icon);
    }

    public void Serve()
    {
        Debug.Log($"{foodName} has been served!");
        Destroy(gameObject);
    }
}
