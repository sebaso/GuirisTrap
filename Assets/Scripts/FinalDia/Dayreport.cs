using UnityEngine;

/// <summary>
/// Contador de las estadísticas del día en curso. Vive dentro del prefab de la
/// pantalla de Stats. Registra durante la jornada:
///   - platos servidos / clientes satisfechos / clientes enfadados
///   - dinero ganado (cobros) y gastado (compras)
/// y calcula la nota del día (F–A).
///
/// No toca SaveManager ni MoneyManager: solo escucha. Se pone a cero al empezar
/// cada día (suscrito a DayManager). Es Singleton para que Client pueda
/// registrar resultados con una sola línea, sin referencias en el inspector.
/// </summary>
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
        // Suscribirse al dinero para registrar ganado/gastado automáticamente.
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChangedDelta += HandleMoneyDelta;

        // Resetear contadores cuando empieza un día nuevo.
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted += ResetCounters;
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChangedDelta -= HandleMoneyDelta;

        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= ResetCounters;
    }

    private void Start()
    {
        // Por si el día ya había arrancado antes de que este objeto existiera.
        ResetCounters();
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

    // --- Registro de dinero (automático vía evento) ---

    private void HandleMoneyDelta(int newBalance, int delta)
    {
        if (delta > 0)      MoneyEarned += delta;
        else if (delta < 0) MoneySpent  += -delta;
    }

    // --- Nota del día ---

    /// <summary>% de clientes satisfechos sobre el total (0–100).</summary>
    public float SatisfactionPercent
    {
        get
        {
            if (TotalClients == 0) return 0f;
            return (ClientsSatisfied / (float)TotalClients) * 100f;
        }
    }

    /// <summary>
    /// Nota del día (F–A). Escala del GDD: A≥90, B≥80, C≥70, D≥60, E≥50, F&lt;50.
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