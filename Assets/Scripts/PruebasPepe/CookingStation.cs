using UnityEngine;

public class CookingStation : MonoBehaviour
{
    [Header("Configuración")]
    public MinigameType stationType; // Nevera (Sartén), Congelador (Horno) o Despensa (Tabla)
    public float interactionDistance = 2.5f; // Radio para que pille la E

    private PlayerController playerRef;
    [SerializeField]
    private Food _food;

    private void Start()
    {
        // Buscamos al jugador al iniciar
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerRef = playerObj.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        if (playerRef == null) return;

        // CALCULAR DISTANCIA ENTRE JUGADOR Y ESTACIÓN
        float dist = Vector3.Distance(transform.position, playerRef.transform.position);

        // PARA CUANDO ESTE CERCA
        if (dist <= interactionDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryStartCooking();
            }
        }
    }

    private void TryStartCooking()
    {
        if (playerRef.currentRecipe != null)
        {
            if (playerRef.currentRecipe.type == stationType)
            {
                Debug.Log($"<color=green>¡Éxito! Iniciando {playerRef.currentRecipe.dishName}</color>");
                
                // QUITAR LOS INGREDIENTES DE ENCIMA
                if (playerRef.redCubeIngredient != null)
                    playerRef.redCubeIngredient.SetActive(false);

                playerRef.CoockFood(_food);

                // LANZAR MINIWEBO CORRESPONDIENTE
                MinigameManager.Instance.LaunchMinigame(playerRef.currentRecipe, playerRef);
                
                // LIMPIAR TODO
                playerRef.currentRecipe = null;
                playerRef.ResetInput(); 
            }
            else
            {
                Debug.Log($"<color=red>c¡CAGASTE! Esta estación no es para {playerRef.currentRecipe.dishName}.</color>");
            }
        }
        else
        {
            Debug.Log("No llevas nada en las manos. LOL");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}