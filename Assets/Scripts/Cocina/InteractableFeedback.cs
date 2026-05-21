using UnityEngine;

public class InteractableFeedback : MonoBehaviour
{
    public Color highlightColor = Color.yellow; 
    private Color originalColor;
    private Renderer targetRenderer;

    void Start()
    {
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