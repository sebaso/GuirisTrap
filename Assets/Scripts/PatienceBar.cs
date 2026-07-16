using UnityEngine;
using UnityEngine.UI;

public class PatienceBar : MonoBehaviour
{
    public Image fillImage;

    [Tooltip("Object shown/hidden and billboarded for the bar. Leave empty to use the fill image's object. " +
             "Must NOT be the Client/Table root — toggling that would disable the whole entity.")]
    public Transform barRoot;

    public Color fullColor  = Color.green;
    public Color halfColor  = Color.yellow;
    public Color emptyColor = Color.red;

    private Client _client; // set when mounted on a client (queue bar)
    private Table _table;   // set when mounted on a table (seated bar)
    private Camera _cam;

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

        if (!shouldShow || fillImage == null) return;

        // Billboard only the bar toward the camera, never the entity it rides on.
        if (_cam != null)
            barRoot.rotation = _cam.transform.rotation;

        fillImage.fillAmount = ratio;
        fillImage.color = Color.Lerp(emptyColor, ratio > 0.5f ? fullColor : halfColor,
                                     ratio > 0.5f ? (ratio - 0.5f) * 2f : ratio * 2f);
    }
}
