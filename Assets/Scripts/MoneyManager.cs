using UnityEngine;
using System;


public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [SerializeField] private int _initialMoney = 500;
    private int _currentMoney;

    /// <summary>Saldo actual.</summary>
    public int CurrentMoney => _currentMoney;

    /// <summary>Alias de CurrentMoney (compatibilidad con el antiguo CashManager.Money).</summary>
    public int Money => _currentMoney;

    /// <summary>Saldo nuevo tras un cambio. Para UI sencilla.</summary>
    public event Action<int> OnMoneyChanged;


    public event Action<int, int> OnMoneyChangedDelta;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _currentMoney = _initialMoney;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Intenta gastar 'amount'. Devuelve true y descuenta si hay saldo; false si no.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;

        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            NotifyChange(-amount);
            AudioManager.Instance?.PlaySFX("money_spent");
            Debug.Log($"[MoneyManager] Gastado {amount}€. Quedan: {_currentMoney}€");
            return true;
        }

        Debug.LogWarning($"[MoneyManager] Dinero insuficiente. Necesitas {amount}€ y tienes {_currentMoney}€");
        AudioManager.Instance?.PlaySFX("money_insufficient");
        return false;
    }

    /// <summary>Ingresa 'amount' al saldo.</summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        _currentMoney += amount;
        NotifyChange(amount);
        AudioManager.Instance?.PlaySFX("money_earned");
        Debug.Log($"[MoneyManager] Ingresado {amount}€. Total: {_currentMoney}€");
    }

    /// <summary>Alias de AddMoney.</summary>
    public void Earn(int amount) => AddMoney(amount);

    /// <summary>Fija el saldo exacto (SaveManager).</summary>
    public void SetMoney(int amount)
    {
        int delta = amount - _currentMoney;
        _currentMoney = amount;
        NotifyChange(delta);
    }

    private void NotifyChange(int delta)
    {
        OnMoneyChanged?.Invoke(_currentMoney);
        OnMoneyChangedDelta?.Invoke(_currentMoney, delta);
    }
}