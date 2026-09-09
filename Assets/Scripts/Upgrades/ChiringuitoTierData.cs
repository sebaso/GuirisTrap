using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

[System.Serializable]
public class ChiringuitoTierData
{
    [Tooltip("Raíz de todo este tier (modelo, zonas, proyecciones y NavMesh si aplica).")]
    public GameObject tierRoot;

    [Tooltip("Botones de cámara (UI) que se muestran a partir de este tier.")]
    public List<GameObject> cameraViewButtons = new();
}