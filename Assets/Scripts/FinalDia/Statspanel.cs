using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class StatsPanel : MonoBehaviour
{
    [Header("Panel raíz")]
    [SerializeField] private GameObject _panelRoot;

    [Header("Textos")]
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

    private bool _subscribed = false;

    private void Awake()
    {
        if (_panelRoot == null) return;

        if (_panelRoot == gameObject)
        {
            //Debug.LogError("[StatsPanel] _panelRoot es el MISMO objeto que tiene el script. " +
          //                 "Debe ser un objeto hijo distinto. Ocultando solo los hijos para no autodesactivarme.");
            SetChildrenActive(false);
        }
        else
        {
            _panelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        TrySubscribe();

        if (_nextDayButton != null)
            _nextDayButton.onClick.AddListener(OnNextDayButton);
    }

    private void OnDisable()
    {
        if (_subscribed && DayManager.Instance != null)
            DayManager.Instance.OnDayEnded -= ShowPanel;
        _subscribed = false;

        if (_nextDayButton != null)
            _nextDayButton.onClick.RemoveListener(OnNextDayButton);
    }

    private void Update()
    {
        // Reintenta suscribirse si el DayManager no existía al activarse el panel.
        if (!_subscribed)
            TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed || DayManager.Instance == null) return;
        DayManager.Instance.OnDayEnded += ShowPanel;
        _subscribed = true;
    }

    /// <summary>Muestra el overlay y rellena los datos. Lo dispara OnDayEnded.</summary>
    public void ShowPanel()
    {
        ShowRoot(true);
        Populate();
        AudioManager.Instance?.PlayStatsMusic();
        AudioManager.Instance?.PlaySFX("day_end");
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
            Debug.LogWarning("[StatsPanel] No hay DayReport.");
        }

        if (_balanceText != null && MoneyManager.Instance != null)
            _balanceText.text = $"{MoneyManager.Instance.CurrentMoney}€";
    }

    // Muestra/oculta el panel sin desactivar nunca el objeto que tiene el script.
    private void ShowRoot(bool visible)
    {
        if (_panelRoot == null) return;

        if (_panelRoot == gameObject)
            SetChildrenActive(visible);  
        else
            _panelRoot.SetActive(visible);
    }

    private void SetChildrenActive(bool visible)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
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
    /// Botón "Siguiente día"
    /// </summary>
    public void OnNextDayButton()
    {
        AudioManager.Instance?.PlaySFX("next_day");
        AudioManager.Instance?.StopMusic(); 

        if (SaveManager.Instance != null)
            SaveManager.Instance.IncrementDayAndSave();

        if (SceneController.Instance != null)
            SceneController.Instance.ChangeScene("PreparationScene");
        else
            Debug.LogError("[StatsPanel] SceneController no encontrado.");
    }
}