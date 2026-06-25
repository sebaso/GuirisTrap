using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class CameraOcclusion : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private LayerMask _wallLayer;
    private List<GameObject> _wallsHitted;

    void Awake()
    {
        _wallsHitted = new List<GameObject>();
    }
    void Update()
    {
        DetectOcclusion();
    }
    private void DetectOcclusion()
    {
        if(_wallsHitted == null) return;

        Vector3 direction = _target.position - transform.position;
        float distance = direction.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, distance, _wallLayer);        
        foreach (RaycastHit hit in hits)
        {
            GameObject wall = hit.collider.gameObject;
            FadeEffectComponent fade = wall.GetComponent<FadeEffectComponent>();
            if (fade == null) continue;
            if (!_wallsHitted.Contains(wall))
            {
                _wallsHitted.Add(wall);
                fade.SetIsOcclusing(true);
            }
        }
        List<GameObject> toRemove = new List<GameObject>();
        foreach (GameObject wall in _wallsHitted)
        {
            bool found = false;
            foreach (RaycastHit hit in hits)
                if (hit.collider.gameObject == wall)
                    found = true;

            if (!found) toRemove.Add(wall);
        }
        foreach (GameObject wall in toRemove)
        {
            wall.GetComponent<FadeEffectComponent>().SetIsOcclusing(false);
            _wallsHitted.Remove(wall);
        }
    }
}