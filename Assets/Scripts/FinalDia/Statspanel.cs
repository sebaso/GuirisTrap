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

    [Header("Demo")]
    [Tooltip("Último día jugable: al completarlo se muestra la pantalla final en vez de volver a preparación.")]
    [SerializeField] private int _demoLastDay = 7;

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

        PopulateWeekSection();
    }

    /// <summary>
    /// Rellena la sección de fin de semana. Solo se muestra si el día que
    /// acaba de terminar cerró una semana (WeekManager.WeekJustEnded).
    /// Todos los campos son opcionales: si no están asignados, no pasa nada.
    /// </summary>
    private void PopulateWeekSection()
    {
        if (_weekResultRoot == null) return;

        WeekManager week = WeekManager.Instance;
        bool show = week != null && week.WeekJustEnded;
        _weekResultRoot.SetActive(show);
        if (!show) return;

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
        {
            SaveManager.Instance.IncrementDayAndSave();

            // Demo de una semana: completado el último día, pantalla final
            // en vez de volver a preparación.
            if (SaveManager.Instance.CurrentDay >= _demoLastDay)
            {
                ShowRoot(false);
                GameOverScreen.Show();
                return;
            }
        }

        if (SceneController.Instance != null)
            SceneController.Instance.ChangeScene("PreparationScene");
        else
            Debug.LogError("[StatsPanel] SceneController no encontrado.");
    }

    [ContextMenu("DEBUG: Terminar la semana y ver el final de la demo")]
    private void DebugEndWeekAndShowGameOver()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[StatsPanel] No hay SaveManager; no se puede cerrar la semana.");
            return;
        }

        // Cierra días hasta el último de la demo: cada cierre registra la nota
        // y las stats del día (WeekManager) y avanza CurrentDay. El día en curso
        // se cierra con lo que lleva; los saltados quedan a cero.
        while (SaveManager.Instance.CurrentDay < _demoLastDay)
        {
            WeekManager.Instance?.OnDayCompleted();
            SaveManager.Instance.IncrementDayAndSave();
            DayReport.Instance?.ResetCounters();
        }

        // Congela lo que quede de día detrás del overlay; el botón del final
        // devuelve al menú y reactiva el tiempo.
        Time.timeScale = 0f;
        ShowRoot(false);
        GameOverScreen.Show();
    }
}