using UnityEngine;
using System;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [SerializeField] private int _initialMoney = 100;
    private int _currentMoney;

    public int CurrentMoney => _currentMoney;

    public event Action<int> OnMoneyChanged;

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

    public bool TrySpend(int amount)
    {
        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            OnMoneyChanged?.Invoke(_currentMoney);
            Debug.Log($"Spent {amount} coins. Remaining: {_currentMoney}");
            return true;
        }

        Debug.LogWarning($"Not enough money! Need {amount}, but only have {_currentMoney}");
        return false;
    }

    public void AddMoney(int amount)
    {
        _currentMoney += amount;
        OnMoneyChanged?.Invoke(_currentMoney);
        Debug.Log($"Gained {amount} coins. Total: {_currentMoney}");
    }
}
