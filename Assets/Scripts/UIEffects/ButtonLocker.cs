using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLocker : MonoBehaviour
{
    [SerializeField] 
    private CanvasGroup _canvasGroup;

    public void SetLocked(bool locked)
    {
        GetComponent<Button>().interactable = !locked;
        _canvasGroup.alpha = locked ? 0.4f : 1f;
    }
}