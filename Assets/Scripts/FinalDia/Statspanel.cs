using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay de fin de día. Vive en un prefab (un Canvas o panel) que se arrastra
/// a la GameScene. Empieza oculto; al recibir DayManager.OnDayEnded se muestra y
/// rellena los datos desde DayReport, SaveManager (día) y MoneyManager (balance).
///
/// El botón "Siguiente día" reproduce lo que antes hacía DayManager.HandleDayEnd:
/// guarda + avanza día con SaveManager.IncrementDayAndSave(), que internamente
/// lleva a la PreparationScene.
///
/// MONTAJE (todo dentro del prefab, así tu compañera solo lo arrastra):
///   - Root del prefab: un GameObject con este script y, debajo, un panel con
///     todos los textos y el botón.
///   - El panel visible debería empezar DESACTIVADO (el script lo activa solo).
///   - Arrastra cada TMP_Text a su campo y el botón a _nextDayButton.
///   - Todos los campos de texto son opcionales: si dejas uno vacío, no se rellena.
///   - Pon también un objeto con DayReport en el prefab (o en la escena).
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("Panel raíz que se muestra/oculta")]
    [Tooltip("El panel visible. Empieza desactivado; el script lo activa al acabar el día.")]
    [SerializeField] private GameObject _panelRoot;

    [Header("Textos (todos opcionales)")]
    [SerializeField] private TMP_Text _dayNumberText;
    [SerializeField] private TMP_Text _dishesServedText;
    [SerializeField] private TMP_Text _moneyEarnedText;
    [SerializeField] private TMP_Text _moneySpentText;
    [SerializeField] private TMP_Text _netMoneyText;
    [SerializeField] private TMP_Text _clientsSatisfiedText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _balanceText;

    [Header("Botón siguiente día")]
    [SerializeField] private Button _nextDayButton;

    [Header("Colores de la nota")]
    [SerializeField] private Color _gradeAColor = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeBColor = new Color(0.50f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeCColor = new Color(0.90f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeDColor = new Color(0.90f, 0.50f, 0.20f);
    [SerializeField] private Color _gradeFColor = new Color(0.80f, 0.20f, 0.20f);

    private void Awake()
    {
        // Empezar oculto.
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayEnded += ShowPanel;

        if (_nextDayButton != null)
            _nextDayButton.onClick.AddListener(OnNextDayButton);
    }

    private void OnDisable()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayEnded -= ShowPanel;

        if (_nextDayButton != null)
            _nextDayButton.onClick.RemoveListener(OnNextDayButton);
    }

    /// <summary>Muestra el overlay y rellena los datos. Lo dispara OnDayEnded.</summary>
    public void ShowPanel()
    {
        if (_panelRoot != null) _panelRoot.SetActive(true);
        Populate();
    }

    private void Populate()
    {
        DayReport report = DayReport.Instance;

        if (_dayNumberText != null && SaveManager.Instance != null)
            _dayNumberText.text = $"DÍA {SaveManager.Instance.CurrentDay}";

        if (report != null)
        {
            if (_dishesServedText != null)
                _dishesServedText.text = $"{report.DishesServed}";

            if (_moneyEarnedText != null)
                _moneyEarnedText.text = $"+{report.MoneyEarned}€";

            if (_moneySpentText != null)
                _moneySpentText.text = $"-{report.MoneySpent}€";

            if (_netMoneyText != null)
            {
                int net = report.NetMoney;
                _netMoneyText.text = net >= 0 ? $"+{net}€" : $"{net}€";
            }

            if (_clientsSatisfiedText != null)
                _clientsSatisfiedText.text = $"{report.ClientsSatisfied}/{report.TotalClients}";

            if (_gradeText != null)
            {
                char grade = report.GetGrade();
                _gradeText.text  = grade.ToString();
                _gradeText.color = GetGradeColor(grade);
            }
        }
        else
        {
            Debug.LogWarning("[StatsPanel] No hay DayReport. ¿Falta el objeto contador en el prefab/escena?");
        }

        if (_balanceText != null && MoneyManager.Instance != null)
            _balanceText.text = $"{MoneyManager.Instance.CurrentMoney}€";
    }

    private Color GetGradeColor(char grade)
    {
        switch (grade)
        {
            case 'A': return _gradeAColor;
            case 'B': return _gradeBColor;
            case 'C': return _gradeCColor;
            case 'D': return _gradeDColor;
            default:  return _gradeFColor; // E y F en rojo
        }
    }

    /// <summary>
    /// Botón "Siguiente día": guarda + avanza día (lo de tu compañero) y va a
    /// preparación. Reemplaza lo que antes hacía DayManager.HandleDayEnd.
    /// </summary>
    public void OnNextDayButton()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IncrementDayAndSave();

        if (SceneController.Instance != null)
            SceneController.Instance.ChangeScene("PreparationScene");
        else
            Debug.LogError("[StatsPanel] SceneController no encontrado.");
    }
}