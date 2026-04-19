using UnityEngine;

// CADA LAYOUT DEL ESPECIERO TIENE ESTE SCRIPT
public class EspecieroLayout : MonoBehaviour
{
    [Tooltip("ARRASTRA LAS ESPECIAS DEL LAYOUT!!!!!!!!!!!!!!!.")]
    public Especia[] especias;

    void Awake()
    {
        if (especias == null || especias.Length == 0)
            especias = GetComponentsInChildren<Especia>();
    }

    public Especia[] GetEspecias() => especias;
}