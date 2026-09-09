using UnityEngine;

// Pon este script en cada panel hijo de Panel_Especias que sea un layout.
// Agrupa las especias y los muros para que EspeciasMinigame los encuentre.
public class EspecieroLayout : MonoBehaviour
{
    [Tooltip("Especias de este layout. Si está vacío se autodetectan.")]
    public EspeciaUI[] especias;

    [Tooltip("RectTransforms de los cubos negros de este layout.")]
    public RectTransform[] cuboNegros;

    void Awake()
    {
        if (especias == null || especias.Length == 0)
            especias = GetComponentsInChildren<EspeciaUI>(true);
    }

    public EspeciaUI[]      GetEspecias()   => especias;
    public RectTransform[]  GetCuboNegros() => cuboNegros;
}
