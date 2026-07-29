using UnityEngine;

// Activo mientras se muestra un diálogo: el input de Interactuar/Aceptar avanza
// el texto en vez de mover al jugador o interactuar con el mundo (comida, muebles...).
public class DialogueControllable : ControllableMonoBehaviour
{
    public override void OnInteractDown(){
            Debug.Log("Interact pulsado en DialogueControllable");

        DialogueManager.Instance?.RequestAdvance();
        }
    public override void OnSubmitDown()   => DialogueManager.Instance?.RequestAdvance();
}
