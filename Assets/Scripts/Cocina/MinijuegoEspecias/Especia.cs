using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Especia : MonoBehaviour
{
    [Header("Waypoints")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Velocidad")]
    public float speed = 2f;

    private int   _currentWaypoint = 0;
    private int   _direction        = 1; // 1 PA ALANTE -1 PA ATRÁS
    private bool  _congelada        = false;
    private Rigidbody _rb;

    public bool IsCongelada => _congelada;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity  = false;
    }

    void Start()
    {
        if (waypoints.Count > 0)
            transform.position = waypoints[0].position;
    }

    void Update()
    {
        if (_congelada || waypoints.Count < 2) return;

        Transform target = waypoints[_currentWaypoint];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
            AdvanceWaypoint();
    }

    private void AdvanceWaypoint()
    {
        int next = _currentWaypoint + _direction;

        if (next >= waypoints.Count || next < 0)
        {
            _direction *= -1; // rebota rebota y en tu cara explota. (Solo rebota. No explota. Era una rima.)
            next = _currentWaypoint + _direction;
        }

        _currentWaypoint = next;
    }

    // Rebote al chocar con CuboNegro o especia congelada
    void OnCollisionEnter(Collision col)
    {
        if (_congelada) return;
        if (col.gameObject.GetComponent<CuboNegro>() != null ||
           (col.gameObject.GetComponent<Especia>()?.IsCongelada ?? false))
        {
            _direction *= -1;
        }
    }

    public void Congelar()
    {
        _congelada = true;
        // DETENER MOVIMIENTO
    }

    // LO LLAMA ESPECIASMINIGAME 
    public void Resetear()
    {
        _congelada        = false;
        _currentWaypoint  = 0;
        _direction        = 1;
        if (waypoints.Count > 0)
            transform.position = waypoints[0].position;
    }
}