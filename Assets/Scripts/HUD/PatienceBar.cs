using UnityEngine;
using UnityEngine.UI;

public class PatienceBar : MonoBehaviour
{
    public Image fillImage;

    public Transform barRoot;

    public Color fullColor  = Color.green;
    public Color halfColor  = Color.yellow;
    public Color emptyColor = Color.red;

    [Header("Pulso de urgencia")]
    [Range(0f, 1f)] public float pulseThreshold = 0.25f;
    public float pulseSpeed = 9f;
    public float pulseAmount = 0.15f;

    private Client _client; // set when mounted on a client (queue bar)
    private Table _table;   // set when mounted on a table (seated bar)
    private Camera _cam;

    private Vector3 _baseScale;
    private bool _baseScaleCaptured = false;

    void Awake()
    {
        _client = GetComponentInParent<Client>();
        _table = GetComponentInParent<Table>();
        _cam = Camera.main;

        // Default the bar root to the fill image's object. Crucially this is NOT
        // this component's GameObject: PatienceBar lives on the Client/Table root,
        // so toggling `gameObject` would disable the whole entity (and once
        // disabled, Update stops and it never comes back).
        if (barRoot == null && fillImage != null) barRoot = fillImage.transform;
    }

    void Update()
    {
        if (barRoot == null) return;

        // Capturar la escala original una sola vez (base del pulso).
        if (!_baseScaleCaptured)
        {
            _baseScale = barRoot.localScale;
            _baseScaleCaptured = true;
        }

        bool shouldShow;
        float ratio;

        if (_table != null)
        {
            // table bar: occupying group's timer, until food arrives
            ClientGroup g = _table.OccupyingGroup;
            shouldShow = g != null && g.IsWaitingForFood;
            ratio = g != null ? g.PatienceRatio : 0f;
        }
        else
        {
            // queue bar: leader only, while waiting
            shouldShow = _client != null && _client.CurrentState == Client.State.Waiting &&
                         (!_client.IsInGroup || _client.IsGroupLeader);
            ratio = _client != null ? _client.PatienceRatio : 0f;
        }

        barRoot.gameObject.SetActive(shouldShow);

        if (!shouldShow || fillImage == null)
        {
            // Nunca dejar la barra "congelada" a mitad de latido.
            if (_baseScaleCaptured) barRoot.localScale = _baseScale;
            return;
        }

        // Billboard only the bar toward the camera, never the entity it rides on.
        if (_cam != null)
            barRoot.rotation = _cam.transform.rotation;

        fillImage.fillAmount = ratio;
        fillImage.color = Color.Lerp(emptyColor, ratio > 0.5f ? fullColor : halfColor,
                                     ratio > 0.5f ? (ratio - 0.5f) * 2f : ratio * 2f);

        // PULSO DE URGENCIA: por debajo del umbral, la barra late. La
        // intensidad crece cuanto más cerca de agotarse está la paciencia
        // (en el umbral apenas se nota; a punto de irse el cliente, late fuerte).
        if (ratio <= pulseThreshold && pulseThreshold > 0f)
        {
            float urgency = 1f - (ratio / pulseThreshold);              // 0 en el umbral → 1 a cero paciencia
            float beat    = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount * urgency;
            barRoot.localScale = _baseScale * (1f + beat);
        }
        else
        {
            barRoot.localScale = _baseScale;
        }
    }
}
