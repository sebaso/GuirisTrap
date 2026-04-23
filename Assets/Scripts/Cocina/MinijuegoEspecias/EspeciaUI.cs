using UnityEngine;
using System.Collections.Generic;

// Cada especia sigue su propio camino de waypoints en orden.
// Al llegar al final (o al rebotar contra un CuboNegro) invierte el recorrido.
public class EspeciaUI : MonoBehaviour
{
    [Header("Camino")]
    [Tooltip("Arrastra aquí los RectTransforms waypoint en orden. La especia los recorre y rebota.")]
    public List<RectTransform> camino = new List<RectTransform>();

    [HideInInspector] public float speed = 100f; // asignado por EspeciasMinigame

    private RectTransform _rect;
    private int _wpIndex  = 0;  // waypoint actual
    private int _dir      = 1;  // 1 = avanzar, -1 = retroceder
    private bool _congelada = false;
    private float _cooldownRebote = 0f;
    public bool IsCongelada => _congelada;
    public RectTransform  Rect => _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (camino.Count > 0 && _rect != null)
            _rect.localPosition = camino[0].localPosition; 
    }

    void Update()
    {
        if (_cooldownRebote > 0f) _cooldownRebote -= Time.deltaTime;

        if (_congelada || camino.Count < 2 || _rect == null) return;

        RectTransform target = camino[_wpIndex];
        _rect.localPosition = Vector3.MoveTowards(
            _rect.localPosition,
            target.localPosition,
            speed * Time.deltaTime
        );

            if (Vector3.Distance(_rect.localPosition, target.localPosition) < 1f)
            AdvanceWaypoint();
    }

    private void AdvanceWaypoint()
    {
        int next = _wpIndex + _dir;

        // Rebote en los extremos del camino
        if (next >= camino.Count || next < 0)
        {
            _dir *= -1;
            next  = _wpIndex + _dir;
        }

        _wpIndex = next;
    }

    // Llamado por EspeciasMinigame al impactar una bala
    public void Congelar() => _congelada = true;

    // Rebote al chocar con CuboNegro (llamado desde EspeciasMinigame)
    public void Rebotar()
    {
        if (_cooldownRebote > 0f) return;
        _dir     *= -1;
        _wpIndex  = Mathf.Clamp(_wpIndex + _dir, 0, camino.Count - 1);
        _cooldownRebote = 0.5f; 
    }

    public void Resetear()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        _congelada = false;
        _dir       = 1;
        _wpIndex   = 0;
        if (camino.Count > 0)
            _rect.localPosition = camino[0].localPosition;
    }
}