using UnityEngine;
using System.Collections.Generic;

public class CameraOcclusion : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private float _fadeSpeed = 5f;
    [SerializeField] private float _minAlpha  = 0.15f;
    [SerializeField] private float _maxAlpha  = 0.4f;

    private Dictionary<Renderer, float> _occluded = new Dictionary<Renderer, float>();
    private Dictionary<Renderer, float> _restoring = new Dictionary<Renderer, float>();

    void Update()
    {
        DetectOcclusion();
        ApplyFades();
    }

    private void DetectOcclusion()
    {
        Vector3 direction = _target.position - transform.position;
        float dist  = direction.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, dist, _wallLayer);

        // Intenta opacar todas las paredes
        foreach (var renderer in _occluded.Keys)
        {
            if (!_restoring.ContainsKey(renderer))
                _restoring[renderer] = GetCurrentAlpha(renderer);
        }
        _occluded.Clear();

        // opacamos las paredes entre el jugador y la camara
        foreach (RaycastHit hit in hits)
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null) 
                continue;

            float wallDist    = Vector3.Distance(transform.position, hit.point);
            float alphaByDist = Mathf.Lerp(_minAlpha,_maxAlpha, wallDist / dist);

            _occluded[renderer] = alphaByDist;
            _restoring.Remove(renderer);
        }
    }

    private void ApplyFades()
    {
        // Aplicamos la transparencia a las paredes que están transparentandose
        foreach (var wall in _occluded)
        {
            float current = GetCurrentAlpha(wall.Key);
            float target  = wall.Value;
            float newAlpha = Mathf.Lerp(current, target, Time.deltaTime * _fadeSpeed);
            SetAlpha(wall.Key, newAlpha);
        }
        

        // Restauramos las paredes que ya no estan entre el jugador y la camara
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var wall in _restoring)
        {
            float current  = GetCurrentAlpha(wall.Key);
            float newAlpha = Mathf.Lerp(current, 1f, Time.deltaTime * _fadeSpeed);
            SetAlpha(wall.Key, newAlpha);

            if (Mathf.Abs(newAlpha - 1f) < 0.01f)
            {
                SetAlpha(wall.Key, 1f);
                toRemove.Add(wall.Key);
            }
        }
        foreach (var renderer in toRemove)
            _restoring.Remove(renderer);
    }

    private float GetCurrentAlpha(Renderer r)
    {
        return r.material.GetFloat("_alpha");
    }

    private void SetAlpha(Renderer r, float alpha)
    {
        r.material.SetFloat("_alpha", alpha);
    }
}