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
            Debug.Log($"[MoneyManager] Gastado {amount}€. Quedan: {_currentMoney}€");
            return true;
        }

        Debug.LogWarning($"[MoneyManager] Dinero insuficiente. Necesitas {amount}€ y tienes {_currentMoney}€");
        return false;
    }

    /// <summary>Ingresa 'amount' al saldo.</summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        _currentMoney += amount;
        NotifyChange(amount);
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

    private void Start()
    {
        // Restaurar el saldo guardado UNA sola vez al arrancar. A partir de aquí
        // el saldo vive en este singleton DontDestroyOnLoad y viaja con el jugador
        // entre escenas (PreparationScene <-> GameScene). NO se recarga desde el
        // guardado al cargar cada escena: eso sobrescribiría el dinero ganado
        // durante el día al llegar a la PreparationScene.
        LoadSavedMoney();
    }

    /// <summary>
    /// Restaura el saldo desde el SaveManager. Se llama una sola vez al arrancar
    /// (Start). El saldo vivo es la fuente de la verdad a partir de ese momento.
    /// </summary>
    private void LoadSavedMoney()
    {
        if (SaveManager.Instance != null && SaveManager.Instance._data.day > 0)
        {
            _currentMoney = SaveManager.Instance._data.money;
            // Avisar solo del balance (sin delta) para refrescar la UI sin
            // disparar popups de "+0€".
            OnMoneyChanged?.Invoke(_currentMoney);
        }
        // Si no hay guardado válido, se mantiene el valor inicial.
    }
}