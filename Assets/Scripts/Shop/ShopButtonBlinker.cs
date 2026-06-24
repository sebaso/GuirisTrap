using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopButtonBlinker : MonoBehaviour
{
    public static Action OnStartBlink;
    public static Action OnStopBlink;

    [SerializeField] private Button _shopButton;
    private Coroutine _blinkCoroutine;

    void OnEnable()
    {
        OnStartBlink += StartBlink;
        OnStopBlink += StopBlink;
    }

    void OnDisable()
    {
        OnStartBlink -= StartBlink;
        OnStopBlink -= StopBlink;
    }

    private void StartBlink()
    {
            Debug.Log("ShopButtonBlinker: StartBlink llamado");

        _blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

    private void StopBlink()
    {
        _shopButton.image.color = Color.white;
        _shopButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
    }

    private IEnumerator BlinkCoroutine()
    {
        while (true)
        {
            _shopButton.image.color = Color.blue;
            _shopButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            yield return new WaitForSeconds(0.5f);
            _shopButton.image.color = Color.white;
            _shopButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
