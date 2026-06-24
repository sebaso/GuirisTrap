using UnityEngine;

[System.Serializable]
public class Condition
{
    // Nombre de la condición (identificador). Debe ser único.
    public string id;
    // Descripción de apoyo.
    [TextArea]
    public string description;
}
