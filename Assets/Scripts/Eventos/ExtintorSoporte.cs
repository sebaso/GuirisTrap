using UnityEngine;

/// <summary>
/// SOPORTE DEL EXTINTOR (colocable/comprable, categoría PlaceableCategory.Extintor).
///
/// Es el mueble de pared que compras en la tienda y colocas en el grid como
/// una silla. De él cuelga un ExtintorPickup cogible. El SOPORTE es permanente
/// (lo compraste); el extintor es el consumible reutilizable: lo coges, apagas
/// un fuego, y vuelve aquí solo para el siguiente viaje.
///
/// El extintor cogible puede ser:
///   · un hijo del prefab del soporte con el componente ExtintorPickup, o
///   · si no hay ninguno, este script instancia _extintorPrefab en el anclaje.
///
/// SETUP:
///   1. Crea el prefab del soporte (tu .fbx de soporte, o un cubo placeholder)
///      con este script y un PlaceableObject, como los demás muebles.
///   2. Cuélgale un hijo con el .fbx del extintor + ExtintorPickup, O asigna
///      _extintorPrefab y un _anchor (Transform vacío donde encaja el extintor).
/// </summary>
public class ExtintorSoporte : MonoBehaviour
{
    [Header("Extintor cogible")]
    [Tooltip("Si el extintor ya es un hijo con ExtintorPickup, déjalo vacío. Si no, este prefab se instancia en el anclaje.")]
    [SerializeField] private GameObject _extintorPrefab;
    [Tooltip("Punto donde descansa el extintor. Si está vacío, se usa el propio soporte.")]
    [SerializeField] private Transform _anchor;

    private ExtintorPickup _extintor;

    void Start()
    {
        Transform anchor = _anchor != null ? _anchor : transform;

        // 1) ¿Ya cuelga un extintor del prefab?
        _extintor = GetComponentInChildren<ExtintorPickup>(true);

        // 2) Si no, instanciarlo desde el prefab.
        if (_extintor == null && _extintorPrefab != null)
        {
            GameObject go = Instantiate(_extintorPrefab, anchor.position, anchor.rotation, anchor);
            _extintor = go.GetComponent<ExtintorPickup>();
            if (_extintor == null) _extintor = go.AddComponent<ExtintorPickup>();
        }

        if (_extintor != null)
            _extintor.AttachToHolder(this, anchor);
        else
            Debug.LogWarning("[ExtintorSoporte] No hay ExtintorPickup ni _extintorPrefab asignado.");
    }

    /// <summary>Punto de descanso del extintor (para que vuelva aquí tras usarse).</summary>
    public Transform RestAnchor => _anchor != null ? _anchor : transform;
}