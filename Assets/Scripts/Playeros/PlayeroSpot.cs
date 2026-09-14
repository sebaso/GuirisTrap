using UnityEngine;

// Punto de actividad reclamable para un Playero: el asiento de una tumbona
// o un sitio a la sombra de una sombrilla. Los crea PlayeroSpawner al
// descubrir los props de la playa; un Playero lo reclama (occupant) mientras
// dura su actividad y lo libera al irse.
public class PlayeroSpot : MonoBehaviour
{
    public enum Kind { Tumbona, Sombra }

    public Kind kind = Kind.Tumbona;

    [HideInInspector] public Playero occupant;

    public bool IsFree => occupant == null;

    // Segundos de actividad que le quedan al ocupante (0 si está libre o el
    // playero no está en actividad): útil para depuración y HUDs.
    public float OccupantActivityRemaining => occupant != null ? occupant.ActivityRemaining : 0f;

    private void OnDrawGizmosSelected()
    {
        if (!IsFree) Gizmos.color = Color.red;
        else Gizmos.color = kind == Kind.Tumbona ? new Color(1f, 0.6f, 0f, 0.9f) : new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
    }
}
