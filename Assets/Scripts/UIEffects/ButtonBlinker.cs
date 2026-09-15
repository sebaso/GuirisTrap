using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBlinker : MonoBehaviour
{
    private Coroutine _blinkCoroutine;
    private Button _button;
    private TextMeshProUGUI _label;

    void Awake()
    {
        _button = GetComponent<Button>();
        _label = _button.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void StartBlink()
    {
        StopBlink();
        _blinkCoroutine = CoroutineRunner.Instance.StartCoroutine(BlinkCoroutine());
    }

    public void StopBlink()
    {
        if (_blinkCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        SetColors(Color.white, Color.black);
    }

    void OnDestroy() => StopBlink();

    private IEnumerator BlinkCoroutine()
    {
        while (true)
        {
            SetColors(Color.blue, Color.white);
            yield return new WaitForSeconds(0.5f);
            SetColors(Color.white, Color.black);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SetColors(Color bg, Color text)
    {
        if (_button != null && _button.image != null) _button.image.color = bg;
        if (_label != null) _label.color = text;
    }
}