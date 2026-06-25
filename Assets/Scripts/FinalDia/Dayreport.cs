using UnityEngine;


public class DayReport : MonoBehaviour
{
    public static DayReport Instance { get; private set; }

    // Contadores del día en curso
    public int DishesServed     { get; private set; }
    public int ClientsSatisfied { get; private set; }
    public int ClientsAngry     { get; private set; }
    public int MoneyEarned      { get; private set; }
    public int MoneySpent       { get; private set; }

    public int TotalClients => ClientsSatisfied + ClientsAngry;
    public int NetMoney     => MoneyEarned - MoneySpent;

    // Suscripción al DayManager para resetear contadores al empezar el día.
    private bool _subscribedDay = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (_subscribedDay && DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= ResetCounters;
        _subscribedDay = false;
    }

    private void Start()
    {
        ResetCounters();
    }

    private void Update()
    {
        if (!_subscribedDay)
            TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (!_subscribedDay && DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += ResetCounters;
            _subscribedDay = true;
        }
    }

    /// <summary>Pone todos los contadores a cero. Se llama al empezar cada día.</summary>
    public void ResetCounters()
    {
        DishesServed     = 0;
        ClientsSatisfied = 0;
        ClientsAngry     = 0;
        MoneyEarned      = 0;
        MoneySpent       = 0;
    }

    // --- Registro desde Client ---

    /// <summary>Un cliente se fue satisfecho. Cuenta como plato servido.</summary>
    public void RegisterSatisfiedClient()
    {
        ClientsSatisfied++;
        DishesServed++;
    }

    /// <summary>Un cliente se fue enfadado (sin pagar).</summary>
    public void RegisterAngryClient()
    {
        ClientsAngry++;
    }

    // --- Registro de dinero (directo, llamado explícitamente) ---

    /// <summary>Registra dinero ganado en el día (pago de un cliente).</summary>
    public void RegisterEarnings(int amount)
    {
        if (amount > 0) MoneyEarned += amount;
    }

    /// <summary>Registra dinero gastado en el día (compras, etc.).</summary>
    public void RegisterSpending(int amount)
    {
        if (amount > 0) MoneySpent += amount;
    }

    // --- Nota del día ---

    /// <summary>% de clientes satisfechos sobre el total (0-100).</summary>
    public float SatisfactionPercent
    {
        get
        {
            if (TotalClients == 0) return 0f;
            return (ClientsSatisfied / (float)TotalClients) * 100f;
        }
    }

    /// <summary>
    /// Nota del día (F-A). Escala del GDD: A>=90, B>=80, C>=70, D>=60, E>=50, F<50.
    /// Si no vino ningún cliente, devuelve 'F'.
    /// </summary>
    public char GetGrade()
    {
        if (TotalClients == 0) return 'F';
        float pct = SatisfactionPercent;
        if (pct >= 90f) return 'A';
        if (pct >= 80f) return 'B';
        if (pct >= 70f) return 'C';
        if (pct >= 60f) return 'D';
        if (pct >= 50f) return 'E';
        return 'F';
    }
}