using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBlinker : MonoBehaviour
{
    private Coroutine _blinkCoroutine;

    public void StartBlink()
    {
        _blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

    public void StopBlink()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = null;
        GetComponent<Button>().image.color = Color.white;
        GetComponent<Button>().GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
    }

    private IEnumerator BlinkCoroutine()
    {
        while (true)
        {
            GetComponent<Button>().image.color = Color.blue;
            GetComponent<Button>().GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            yield return new WaitForSeconds(0.5f);
            GetComponent<Button>().image.color = Color.white;
            GetComponent<Button>().GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
