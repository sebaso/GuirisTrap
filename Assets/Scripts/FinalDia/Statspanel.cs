using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class StatsPanel : MonoBehaviour
{
    [Header("Panel raíz")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private GameObject _dailyStatsPanel;

    [Header("Textos")]
    [SerializeField] private TMP_Text _dayNumberText;
    [SerializeField] private TMP_Text _dishesServedText;
    [SerializeField] private TMP_Text _moneyEarnedText;
    [SerializeField] private TMP_Text _moneySpentText;
    [SerializeField] private TMP_Text _netMoneyText;
    [SerializeField] private TMP_Text _clientsSatisfiedText;
    [SerializeField] private TMP_Text _balanceText;

    [Header("Nota del día")]
    [SerializeField] private Image _gradeImage;

    [Header("Resumen semanal")]
    [SerializeField] private GameObject _weekResultRoot;
    [SerializeField] private TMP_Text _weekAverageText;
    [SerializeField] private Image _weekAverageGradeImage;
    [SerializeField] private TMP_Text _weekStarsText;
    [SerializeField] private TMP_Text _weekBonusText;

    [Header("Sprites de nota (A-F)")]
    [SerializeField] private Sprite _gradeASprite;
    [SerializeField] private Sprite _gradeBSprite;
    [SerializeField] private Sprite _gradeCSprite;
    [SerializeField] private Sprite _gradeDSprite;
    [SerializeField] private Sprite _gradeESprite;
    [SerializeField] private Sprite _gradeFSprite;

    [Header("Botón siguiente día")]
    [SerializeField] private Button _nextDayButton;
    [SerializeField] private TMP_Text _nextDayButtonLabel;
    private bool _awaitingWeekSummaryTap = false;

    [Header("Colores de la nota")]
    [SerializeField] private Color _gradeAColor = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeBColor = new Color(0.50f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeCColor = new Color(0.90f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeDColor = new Color(0.90f, 0.50f, 0.20f);
    [SerializeField] private Color _gradeFColor = new Color(0.80f, 0.20f, 0.20f);

    private bool _subscribed = false;

    private void Awake()
    {
        DayReport dr = GetComponentInChildren<DayReport>(true);
        if (dr != null && !dr.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[StatsPanel] '{dr.name}' está dentro de un objeto DESACTIVADO " +
                           "('" + dr.transform.parent?.name + "'). Sácalo fuera del panel o " +
                           "actívalo, o el informe del día saldrá vacío.", dr.gameObject);
        }

        if (_panelRoot == null) return;

        if (_panelRoot == gameObject)
        {
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
    }

    private void OnDisable()
    {
        if (_subscribed && DayManager.Instance != null)
            DayManager.Instance.OnDayEnded -= ShowPanel;
        _subscribed = false;

        _nextDayButton?.onClick.RemoveAllListeners();
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

        // Congelar como en el menú de pausa: no debes poder moverte ni
        // interactuar mientras lees el resumen del día.
        Time.timeScale = 0f;
        InputManager.Instance?.EnterPause();

        AudioManager.Instance?.PlayStatsMusic();
        AudioManager.Instance?.PlaySFX("day_end");
    }

    private void OnDestroy()
    {
        // Si la escena se descarga con el panel abierto, la siguiente arrancaría
        // congelada.
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
            InputManager.Instance?.ExitPause();
        }
    }

    private void Populate()
    {
        _dailyStatsPanel?.SetActive(true);

        DayReport report = DayReport.Instance;

        if (_dayNumberText != null && SaveManager.Instance != null)
        {
            int playingDay = SaveManager.Instance.CurrentDay + 1;
            _dayNumberText.text = $"DÍA {playingDay} · {WeekManager.GetDayName(playingDay)}";
        }

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

            if (_gradeImage != null)
            {
                char grade = report.GetGrade();
                _gradeImage.sprite = GetGradeSprite(grade);
            }
        }
        else
        {
            Debug.LogWarning("[StatsPanel] No hay DayReport.");
        }

        if (_balanceText != null && MoneyManager.Instance != null)
            _balanceText.text = $"{MoneyManager.Instance.CurrentMoney}€";

        bool weekJustEnded = WeekManager.Instance != null && WeekManager.Instance.WeekJustEnded;

        PopulateWeekSection();
        SetupNextDayButton();
    }

    private void SetupNextDayButton()
    {
        if (_nextDayButton == null) return;

        _nextDayButton.onClick.RemoveAllListeners();

        bool weekJustEnded = WeekManager.Instance != null && WeekManager.Instance.WeekJustEnded;

        if (weekJustEnded)
        {
            _awaitingWeekSummaryTap = true;
            if (_nextDayButtonLabel != null) _nextDayButtonLabel.text = "Resumen Semanal";
            _nextDayButton.onClick.AddListener(OnViewWeekSummaryButton);
        }
        else
        {
            _awaitingWeekSummaryTap = false;
            if (_nextDayButtonLabel != null) _nextDayButtonLabel.text = "Siguiente Día";
            _nextDayButton.onClick.AddListener(OnNextDayButton);
        }
    }
    private void PopulateWeekSection()
    {
        if (_weekResultRoot == null) return;

        WeekManager week = WeekManager.Instance;
        bool weekJustEnded = week != null && week.WeekJustEnded;

        _weekResultRoot.SetActive(false);
        if (!weekJustEnded) return;

        WeekResult r = week.LastResult;

        if (_weekAverageText != null)
            _weekAverageText.text = $"FIN DE SEMANA {r.weekNumber}";

        if (_weekAverageGradeImage != null)
            _weekAverageGradeImage.sprite = GetGradeSprite(r.averageGrade);

        if (_weekStarsText != null)
        {
            float delta = r.StarsDelta;
            string deltaTxt = delta > 0f ? $" (+{delta:0.##})"
                            : delta < 0f ? $" ({delta:0.##})"
                            : " (=)";
            _weekStarsText.text = $"Estrellas {r.starsBefore:0.##} → {r.starsAfter:0.##}{deltaTxt}";
        }

        if (_weekBonusText != null)
            _weekBonusText.text = r.moneyBonus > 0 ? $"BONUS: +{r.moneyBonus}€" : string.Empty;
    }

    private void OnViewWeekSummaryButton()
    {
        _awaitingWeekSummaryTap = false;
        if (_dailyStatsPanel != null) _dailyStatsPanel.SetActive(false); // ← nuevo
        _weekResultRoot.SetActive(true);
        if (_nextDayButtonLabel != null) _nextDayButtonLabel.text = "SIGUIENTE SEMANA";

        _nextDayButton.onClick.RemoveAllListeners();
        _nextDayButton.onClick.AddListener(OnNextDayButton);
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
        {
            if (child.GetComponent<DayReport>() != null) continue;

            child.gameObject.SetActive(visible);
        }
    }

    private Sprite GetGradeSprite(char grade)
    {
        switch (grade)
        {
            case 'A': return _gradeASprite;
            case 'B': return _gradeBSprite;
            case 'C': return _gradeCSprite;
            case 'D': return _gradeDSprite;
            case 'E': return _gradeESprite;
            default:  return _gradeFSprite;
        }
    }

    /// <summary>
    /// Botón "Siguiente día"
    /// </summary>
    public void OnNextDayButton()
    {
        // Descongelar ANTES de cambiar de escena, o la siguiente carga parada.
        Time.timeScale = 1f;
        InputManager.Instance?.ExitPause();

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