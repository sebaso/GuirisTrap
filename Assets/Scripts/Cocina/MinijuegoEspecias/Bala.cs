using UnityEngine;

public class Bala : MonoBehaviour
{
    public float speed = 10f;
    private Vector3 _direction = Vector3.up;
    private EspeciasMinigame _minigame;

    public void Init(Vector3 direction, EspeciasMinigame minigame)
    {
        _direction = direction.normalized;
        _minigame  = minigame;
    }

    void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // Golpea especia -> la SUPER congela
        Especia especia = other.GetComponent<Especia>();
        if (especia != null && !especia.IsCongelada)
        {
            especia.Congelar();
            _minigame.OnBalaImpacta();
            Destroy(gameObject);
            return;
        }

        // Golpea cubo negro → A TOMAR POR CULO
        if (other.GetComponent<CuboNegro>() != null)
        {
            _minigame.OnBalaImpacta();
            Destroy(gameObject);
            return;
        }

        // Golpea especia congelada → A TOMAR POR CULO (como cubo negro)
        if (especia != null && especia.IsCongelada)
        {
            _minigame.OnBalaImpacta();
            Destroy(gameObject);
            return;
        }

        // Golpea techo (límite superior) → A TOMAR POR CULO 
        if (other.CompareTag("TechoBala"))
        {
            _minigame.OnBalaImpacta();
            Destroy(gameObject);
        }
    }
}