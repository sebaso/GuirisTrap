using UnityEngine;

// EL CUBO NEGRO SOLO SE USA EN EL MINIJUEGO DE ESPECIAS
// NO SE LE PONE EL SCRIPT AL CUBO SE HACE EN LA PARTE DEL INSPECTOR DE LISTAR................

public class CuboNegro : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}