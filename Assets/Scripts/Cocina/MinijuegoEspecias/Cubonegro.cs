using UnityEngine;

// EL CUBO NEGRO SOLO SE USA EN EL MINIJUEGO DE ESPECIAS
// LE PONES COLLIDER TRIGGER FALSO Y ESTO, ENTONCES REBOTAN Y DESAPARECEN
public class CuboNegro : MonoBehaviour
{
    // Marca visual en el editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}