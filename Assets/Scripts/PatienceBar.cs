using UnityEngine;
using UnityEngine.UI;

public class PatienceBar : MonoBehaviour
{
    public Image fillImage;

    public Color fullColor  = Color.green;
    public Color halfColor  = Color.yellow;
    public Color emptyColor = Color.red;

    private Client _client;
    private Camera _cam;

    void Awake()
    {
        _client = GetComponentInParent<Client>();
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

        bool shouldShow = _client != null &&
                          (_client.CurrentState == Client.State.WaitingForFood);

        gameObject.SetActive(shouldShow);

        if (!shouldShow || fillImage == null) return;

        float ratio = _client != null ? _client.PatienceRatio : 1f;
        fillImage.fillAmount = ratio;
        fillImage.color = Color.Lerp(emptyColor, ratio > 0.5f ? fullColor : halfColor,
                                     ratio > 0.5f ? (ratio - 0.5f) * 2f : ratio * 2f);
    }
}
