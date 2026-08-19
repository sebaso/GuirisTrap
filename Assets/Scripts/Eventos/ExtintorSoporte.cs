using UnityEngine;

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