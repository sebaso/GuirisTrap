using System.Collections;
using UnityEngine;


public class SpecialClientTag : MonoBehaviour
{
    public SpecialClientData Data { get; private set; }

    private Client _client;
    private ClientGroup _group;
    private Client.State _prevState;
    private bool _orderApplied;
    private bool _verdictScheduled;

    /// <summary>Lo llama SpecialClientManager justo después de instanciar.</summary>
    public void Setup(SpecialClientData data, ClientGroup group)
    {
        Data = data;
        _group = group;
        _client = GetComponent<Client>();
        _prevState = _client != null ? _client.CurrentState : Client.State.WalkingToEntrance;

        if (Data != null && Data.visualPrefab != null)
            StartCoroutine(SwapVisualRoutine());
    }

    void Update()
    {
        if (_client == null || Data == null) return;

        Client.State s = _client.CurrentState;
        if (s == _prevState) return;

        Client.State from = _prevState;
        _prevState = s;
        OnStateChanged(from, s);
    }

    private void OnStateChanged(Client.State from, Client.State to)
    {
        bool isLeader = !_client.IsInGroup || _client.IsGroupLeader;

        switch (to)
        {
            case Client.State.WaitingForFood:
                // Recién sentado: el grupo ya tiene su pedido generado
                // (RestaurantManager.SeatGroup lo hace antes de que se sienten).
                if (isLeader && !_orderApplied)
                {
                    _orderApplied = true;
                    SpecialClientManager.Instance?.ApplySpecialOrder(_group, Data);
                    SpecialClientManager.Instance?.PlayLines(Data, Data.entryLines);
                }
                break;

            case Client.State.Eating:
                // Le han servido: dentro de un rato se juzga la decoración.
                if (Data.HasDecorCondition && !_verdictScheduled)
                {
                    _verdictScheduled = true;
                    StartCoroutine(DecorVerdictRoutine());
                }
                break;

            case Client.State.DoneEating:
                // Ha terminado y ha pagado (entero si la decoración estaba,
                // rebajado si no). El líder narra el desenlace.
                if (isLeader)
                {
                    bool happy = SpecialClientManager.Instance == null
                              || SpecialClientManager.Instance.GroupIsHappy(_group);

                    if (happy) SpecialClientManager.Instance?.OnSpecialSatisfied(_client, Data);
                    else       SpecialClientManager.Instance?.OnSpecialUnhappy(_client, Data);
                }
                break;
        }
    }


    private IEnumerator DecorVerdictRoutine()
    {
        yield return new WaitForSeconds(_client.eatDuration * 0.7f);

        if (_client == null) yield break;
        if (_client.CurrentState != Client.State.Eating) yield break; // ya se fue por otro camino

        bool ok = SpecialClientManager.Instance == null
               || SpecialClientManager.Instance.EvaluateDecorCondition(_group, Data);
        if (ok) yield break; // pagarán entero y se irán contentos

        // Rebajar lo que paga este miembro. Cada tag ajusta el suyo, así que
        // el descuento se aplica a todo el grupo.
        int before = _client.money;
        _client.money = Mathf.Max(0, Mathf.RoundToInt(before * Data.unhappyPaymentMultiplier));

        Debug.Log($"[SpecialClientTag] {Data.clientName}: decoración incompleta, " +
                  $"paga {_client.money}€ en vez de {before}€.");

        // Si el multiplicador es 0, ni se quedan: se largan sin pagar.
        if (_client.money <= 0)
        {
            if (!_client.IsInGroup || _client.IsGroupLeader)
                SpecialClientManager.Instance?.OnSpecialUnhappy(_client, Data);
            _client.LeaveAngrySelf();
        }

    }

    /// <summary>Sustituye el modelo aleatorio por el del cliente especial en
    /// cuanto Client.initialize() lo haya creado.</summary>
    private IEnumerator SwapVisualRoutine()
    {
        float timeout = Time.time + 5f;
        while (transform.childCount == 0 && Time.time < timeout)
            yield return null;

        // Fuera los modelos aleatorios que haya instanciado el cliente base.
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        // Mismo tratamiento que Client.initialize() da a sus modelos.
        GameObject model = Instantiate(Data.visualPrefab, transform.position, Quaternion.identity);
        model.transform.SetParent(transform);
        model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        model.transform.position += new Vector3(0f, -0.5f, 0f);
    }
}