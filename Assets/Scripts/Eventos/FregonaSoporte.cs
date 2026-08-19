using UnityEngine;


public class FregonaSoporte : MonoBehaviour
{
    [Header("Fregona cogible")]
    [SerializeField] private GameObject _fregonaPrefab;
    [SerializeField] private Transform _anchor;

    private FregonaPickup _fregona;

    void Start()
    {
        Transform anchor = _anchor != null ? _anchor : transform;

        // 1) ¿Ya cuelga una fregona del prefab?
        _fregona = GetComponentInChildren<FregonaPickup>(true);

        // 2) Si no, instanciarla desde el prefab.
        if (_fregona == null && _fregonaPrefab != null)
        {
            GameObject go = Instantiate(_fregonaPrefab, anchor.position, anchor.rotation, anchor);
            _fregona = go.GetComponent<FregonaPickup>();
            if (_fregona == null) _fregona = go.AddComponent<FregonaPickup>();
        }

        if (_fregona != null)
            _fregona.AttachToHolder(this, anchor);
        else
            Debug.LogWarning("[FregonaSoporte] No hay FregonaPickup ni _fregonaPrefab asignado.");
    }

    /// <summary>Punto de descanso de la fregona.</summary>
    public Transform RestAnchor => _anchor != null ? _anchor : transform;
}
