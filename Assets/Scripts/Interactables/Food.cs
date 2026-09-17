using System.Collections.Generic;
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
    // Comensales que este plato alimentó: cuando todos terminan, el plato se
    // consume (antes no había ningún consumo: los platos vivían para siempre).
    private readonly List<Client> _fedMembers = new List<Client>();
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
            _servedIcon = FoodIcon.Create(transform, ServedIconHeight(), 0.35f);
        _servedIcon.Set(icon);
    }

    // El plato se cuelga del punto de comida —o de la raíz de la mesa si
    // foodPoint no está asignado (pivote en el SUELDO)—, así que la altura del
    // icono se mide desde la tapa real de la mesa (colliders activos) para no
    // quedar dentro del mueble.
    private float ServedIconHeight()
    {
        float foodY = transform.position.y;
        float top = foodY + 0.35f;
        Table table = GetComponentInParent<Table>();
        if (table != null)
        {
            foreach (Collider col in table.GetComponentsInChildren<Collider>())
            {
                if (col == null || !col.enabled || col.isTrigger) continue;
                if (col.transform.IsChildOf(transform)) continue; // platos, no la mesa
                top = Mathf.Max(top, col.bounds.max.y + 0.12f);
            }
        }
        return top - foodY;
    }

    public void RegisterFedMember(Client diner)
    {
        if (diner != null) _fedMembers.Add(diner);
    }

    // Plato comido: cuando TODOS los comensales a los que alimentó terminan
    // (o se van enfadados/expulsados), el plato desaparece de la mesa.
    void Update()
    {
        if (!isServed || _fedMembers.Count == 0) return;

        foreach (Client diner in _fedMembers)
        {
            if (diner == null) continue;
            Client.State s = diner.CurrentState;
            if (s != Client.State.DoneEating && s != Client.State.Leaving && s != Client.State.Angry)
                return; // alguien aún lo está comiendo
        }

        Debug.Log($"[Food] {foodName} eaten — clearing plate.");
        Destroy(gameObject);
    }

    public void Serve()
    {
        Debug.Log($"{foodName} has been served!");
        Destroy(gameObject);
    }
}
