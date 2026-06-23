using UnityEngine;
using UnityEngine.UI;

public class PatienceBar : MonoBehaviour
{
    public Image fillImage;

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
    }

    void Update()
    {
        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

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

        gameObject.SetActive(shouldShow);

        if (!shouldShow || fillImage == null) return;

        fillImage.fillAmount = ratio;
        fillImage.color = Color.Lerp(emptyColor, ratio > 0.5f ? fullColor : halfColor,
                                     ratio > 0.5f ? (ratio - 0.5f) * 2f : ratio * 2f);
    }
}
