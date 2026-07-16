using UnityEngine;

public class CookingStation : MonoBehaviour
{
    [Header("Configuración")]
    public MinigameType stationType;
    public float        interactionDistance = 2.5f;

    private PlayerController playerRef;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerRef = playerObj.GetComponent<PlayerController>();
    }

    public void TryInteract()
    {
        if (playerRef == null || playerRef.currentRecipe == null)
        {
            Debug.Log("No llevas nada en las manos.");
            HUDMessage.Instance?.ShowWarning("No llevas nada en las manos. Saca una receta de la nevera/despensa primero.");
            return;
        }

        if (playerRef.currentRecipe.type == stationType)
        {
            Debug.Log($"<color=green>¡Éxito! Iniciando {playerRef.currentRecipe.dishName}</color>");
            HUDMessage.Instance?.ShowGood($"¡Cocinando {playerRef.currentRecipe.dishName}!");
            if (playerRef.redCubeIngredient != null)
                playerRef.redCubeIngredient.SetActive(false);

            MinigameManager.Instance.LaunchMinigame(playerRef.currentRecipe, playerRef);
            playerRef.currentRecipe = null;
        }
        else
        {
            Debug.Log($"<color=red>Esta estación no es para {playerRef.currentRecipe.dishName}.</color>");
            HUDMessage.Instance?.ShowBad($"Esta estación no es para {playerRef.currentRecipe.dishName}.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}