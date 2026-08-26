using System.Collections;
using UnityEngine;

// Se añade en runtime al cliente especial. Observa Client.CurrentState desde
// fuera y reacciona, para no meter campos ni hooks dentro de Client.cs.

public class SpecialClientTag : MonoBehaviour
{
    public SpecialClientData Data { get; private set; }

    private Client _client;
    private ClientGroup _group;
    private Client.State _prevState;
    private bool _orderApplied;
    private bool _verdictScheduled;

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
        switch (to)
        {
            case Client.State.WaitingForFood:
                // Recién sentado: el grupo ya tiene su pedido generado
                // (RestaurantManager.SeatGroup lo hace antes de que se sienten).
                //
                // Lo intenta CUALQUIER miembro, no solo el líder: si la mesa
                // tiene menos capacidad que el grupo puede que al líder no le
                // toque silla, y entonces el pedido nunca se aplicaría. El
                // manager lo hace idempotente y solo devuelve true la primera
                // vez, que es cuando salta el diálogo de entrada.
                if (!_orderApplied)
                {
                    _orderApplied = true;
                    bool first = SpecialClientManager.Instance != null
                              && SpecialClientManager.Instance.ApplySpecialOrder(_group, Data);

                    if (first)
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
                // rebajado si no). Narra el PRIMERO que llegue, no el líder:
                // igual que con el pedido, el líder podría no estar sentado.
                if (SpecialClientManager.Instance != null &&
                    SpecialClientManager.Instance.TryClaimOutcome(_group))
                {
                    if (SpecialClientManager.Instance.GroupIsHappy(_group))
                        SpecialClientManager.Instance.OnSpecialSatisfied(_client, Data);
                    else
                        SpecialClientManager.Instance.OnSpecialUnhappy(_client, Data);
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
            if (SpecialClientManager.Instance != null &&
                SpecialClientManager.Instance.TryClaimOutcome(_group))
                SpecialClientManager.Instance.OnSpecialUnhappy(_client, Data);

            _client.LeaveAngrySelf();
        }
        // Si no, siguen comiendo: Client.FinishEating cobrará la cantidad
        // rebajada y los contará como descontentos, y el diálogo de queja salta
        // al pasar a DoneEating.
    }

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
