using UnityEngine;

/// <summary>
/// Singleton that tracks the player's money.
/// Call TrySpend() before purchases and Earn() when clients pay.
/// </summary>
public class CashManager : MonoBehaviour
{
    public static CashManager Instance { get; private set; }

    [Header("Economy")]
    [SerializeField] private int _startingMoney = 500;

    private int _money;

    /// <summary>Current money balance.</summary>
    public int Money => _money;

    /// <summary>
    /// Fired whenever money changes.
    /// Args: (newBalance, delta) — delta is negative for a spend, positive for an earn.
    /// </summary>
    public System.Action<int, int> OnMoneyChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _money = _startingMoney;
    }

    /// <summary>
    /// Attempt to spend <paramref name="amount"/>.
    /// Returns true and deducts money if the balance is sufficient; returns false otherwise.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;

        if (_money < amount)
        {
            Debug.Log($"[CashManager] Not enough money. Have: {_money}€, Need: {amount}€");
            return false;
        }

        _money -= amount;
        OnMoneyChanged?.Invoke(_money, -amount);
        Debug.Log($"[CashManager] Spent {amount}€. Balance: {_money}€");
        return true;
    }

    /// <summary>Adds <paramref name="amount"/> to the balance.</summary>
    public void Earn(int amount)
    {
        if (amount <= 0) return;
        _money += amount;
        OnMoneyChanged?.Invoke(_money, amount);
        Debug.Log($"[CashManager] Earned {amount}€. Balance: {_money}€");
    }
}
