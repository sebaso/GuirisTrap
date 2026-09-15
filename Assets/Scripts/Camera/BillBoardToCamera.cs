using UnityEngine;

// Hace que un sprite mire siempre a la cámara que lo está dibujando.
//
// La gracia está en usar OnWillRenderObject: Unity lo llama UNA VEZ POR CÁMARA
// justo antes de dibujar el objeto, con Camera.current apuntando a esa cámara.
// Orientarse ahí resuelve dos cosas de golpe:
//
//  - En la VISTA DE ESCENA del editor se orientan a la cámara de escena, así que
//    se ven bien mientras trabajas. Si se orientan a Camera.main (como hacía la
//    versión anterior), en la vista de escena salen torcidos y parece que hay un
//    bug donde no lo hay.
//  - Si algún día hay varias cámaras (minimapa, cinemática), cada una los ve
//    bien sin tener que hacer nada.

[DisallowMultipleComponent]
public class BillboardToCamera : MonoBehaviour
{
    public enum Modo
    {
        /// <summary>Paralelo a la pantalla. Es lo normal para cielo y nubes:
        /// todos comparten orientación y no se retuercen entre ellos.</summary>
        PlanoDePantalla,
        /// <summary>Gira solo sobre su eje vertical, como un cartel. Útil para
        /// cosas plantadas en el suelo que no deben tumbarse.</summary>
        SoloEjeY,
    }

    [SerializeField] private Modo _modo = Modo.PlanoDePantalla;

    void OnWillRenderObject()
    {
        Camera cam = Camera.current;
        if (cam == null) return;

        if (_modo == Modo.PlanoDePantalla)
        {
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }
        else
        {
            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}