using UnityEngine;

public class InteractableFeedback : MonoBehaviour
{
    public Color highlightColor = Color.yellow; // Color al estar cerca
    private Color originalColor;
    private Renderer targetRenderer;

    void Start()
    {
        // Buscamos el renderer en este objeto o en sus hijos
        targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            originalColor = targetRenderer.material.color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetRenderer != null)
        {
            targetRenderer.material.color = highlightColor;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && targetRenderer != null)
        {
            targetRenderer.material.color = originalColor;
        }
    }
}