using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Pantalla de fin de día. Al mostrarse ya no aparece de golpe: el ticket baja
/// desde arriba de la pantalla a tirones, como una impresora de tickets
/// (primer tramo del 30%, parón, otro del 20%, parón...), con todos los
/// valores de las estadísticas VACÍOS. Una vez colgado, la "impresora" va
/// rellenando cada valor carácter a carácter. Cualquier pulsación de E
/// (Interact) completa al instante la bajada y la impresión.
/// </summary>
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
    [SerializeField] private TMP_Text _weekBonusText;
    [System.Serializable]
    public class StarColorSprites
    {
        public Sprite empty;
        public Sprite half;
        public Sprite full;
    }

    [Header("Estrellas (sprites)")]
    [SerializeField] private Image[] _starImages;
    [SerializeField] private StarColorSprites _goldStars;
    [SerializeField] private StarColorSprites _greenStars;
    [SerializeField] private StarColorSprites _redStars;
    [SerializeField] private Sprite _halfGoldGreenSprite;
    [SerializeField] private Sprite _halfGoldRedSprite;

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
    [SerializeField] private RectTransform _nextDayButtonRect;
    private bool _awaitingWeekSummaryTap = false;
    private Vector3 _nextDayButtonDefaultPos;
    private static readonly Vector3 WeekSummaryButtonPos = new Vector3(382f, -468f, 0f);

    [Header("Demo")]
    [Tooltip("Último día jugable: al completarlo se muestra la pantalla final en vez de volver a preparación.")]
    [SerializeField] private int _demoLastDay = 7;

    [Header("Colores de la nota")]
    [SerializeField] private Color _gradeAColor = new(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeBColor = new(0.50f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeCColor = new(0.90f, 0.80f, 0.20f);
    [SerializeField] private Color _gradeDColor = new(0.90f, 0.50f, 0.20f);
    [SerializeField] private Color _gradeFColor = new(0.80f, 0.20f, 0.20f);

    [Header("Bajada tipo impresora")]
    [Tooltip("Cuánto baja el ticket en cada tirón, en % de la distancia total " +
             "(se normalizan; por defecto 30, luego 20, y así hasta el 100%).")]
    [SerializeField] private float[] _dropStepPercents = { 30f, 20f, 15f, 10f, 10f, 8f, 7f };
    [Tooltip("Segundos que dura cada tirón de bajada.")]
    [SerializeField, Min(0.01f)] private float _dropStepSeconds = 0.12f;
    [Tooltip("Pausa del motor entre tirón y tirón.")]
    [SerializeField, Min(0f)] private float _dropStepPause = 0.18f;
    [Tooltip("Sobretiro hacia abajo al acabar cada tirón, como fracción del " +
             "tamaño del tramo (0 = sin rebote).")]
    [SerializeField, Range(0f, 0.5f)] private float _dropRecoilPercent = 0.06f;
    [Tooltip("Segundos del retroceso tras el sobretiro.")]
    [SerializeField, Min(0f)] private float _dropRecoilSeconds = 0.08f;
    [Tooltip("Margen extra (unidades de canvas) para que el ticket arranque " +
             "del todo fuera de la pantalla, por arriba.")]
    [SerializeField, Min(0f)] private float _dropStartMargin = 32f;

    [Header("Relleno de valores (máquina de escribir)")]
    [Tooltip("Segundos entre carácter y carácter de cada valor.")]
    [SerializeField, Min(0.005f)] private float _typeCharSeconds = 0.045f;
    [Tooltip("Pausa entre línea y línea del ticket.")]
    [SerializeField, Min(0f)] private float _typeLinePause = 0.28f;
    [Tooltip("Mostrar un cursor '_' mientras se imprime cada valor.")]
    [SerializeField] private bool _showTypeCursor = true;
    [Tooltip("Segundos que tarda en 'sellar' la nota al final.")]
    [SerializeField, Min(0f)] private float _stampSeconds = 0.14f;
    [Tooltip("Etiqueta del botón mientras el ticket se imprime (vacío = no tocarla).")]
    [SerializeField] private string _printingButtonLabel = "Pulsa E para saltar";

    [Header("SFX de la impresora")]
    [Tooltip("Sonido de cada tirón de bajada.")]
    [SerializeField] private string _dropSfx = "switch3";
    [Tooltip("Sonido de cada carácter impreso.")]
    [SerializeField] private string _typeSfx = "click4";
    [Tooltip("Sonido al sellar la nota.")]
    [SerializeField] private string _stampSfx = "switch1";
    [Tooltip("Sonido al terminar una línea de dinero.")]
    [SerializeField] private string _moneySfx = "money_earned";

    private bool _subscribed = false;
    private bool _skipSubscribed = false;

    // --- Estado de la secuencia de entrada ---

    /// <summary>Una línea del ticket que se imprime: un texto final y su campo.</summary>
    private class PrintLine
    {
        public TMP_Text target;
        public string finalText;
        public bool moneyLine;   // suena "money_earned" al terminarla
        public bool isGrade;     // no se teclea: la nota se "sella"
    }

    private readonly List<PrintLine> _printLines = new List<PrintLine>();
    private Coroutine _sequenceRoutine;
    private int _sequenceStartFrame = -1;

    private RectTransform _panelRect;
    private Vector2 _panelHomeAnchoredPos;
    private Vector3 _gradeHomeScale = Vector3.one;
    private readonly Vector3[] _worldCorners = new Vector3[4];

    private void Awake()
    {
        _panelRect = (_panelRoot != null ? _panelRoot.transform : transform) as RectTransform;
        if (_panelRect == null) _panelRect = transform as RectTransform;
        if (_panelRect != null)
            _panelHomeAnchoredPos = _panelRect.anchoredPosition;
        if (_gradeImage != null)
            _gradeHomeScale = _gradeImage.transform.localScale;

        if (_nextDayButtonRect != null)
            _nextDayButtonDefaultPos = _nextDayButtonRect.anchoredPosition3D;

        DayReport dr = GetComponentInChildren<DayReport>(true);
        if (dr != null && !dr.gameObject.activeInHierarchy)
        {
            string parentName = dr.transform.parent != null ? dr.transform.parent.name : "<raíz>";
            Debug.LogError($"[StatsPanel] '{dr.name}' está dentro de un objeto DESACTIVADO " +
                           $"('{parentName}'). Sácalo fuera del panel o actívalo, o el informe " +
                           "del día saldrá vacío.", dr.gameObject);
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
        TrySubscribeSkip();
    }

    private void OnDisable()
    {
        if (_subscribed && DayManager.Instance != null)
            DayManager.Instance.OnDayEnded -= ShowPanel;
        _subscribed = false;

        if (_skipSubscribed && InputManager.Instance != null)
            InputManager.Instance.OnInteractPressed -= OnInteractPressedSkip;
        _skipSubscribed = false;

        StopSequence();

        if (_nextDayButton != null)
            _nextDayButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        // Reintenta suscribirse si el DayManager/InputManager no existían aún.
        if (!_subscribed)
            TrySubscribe();
        if (!_skipSubscribed)
            TrySubscribeSkip();
    }

    private void TrySubscribe()
    {
        if (_subscribed || DayManager.Instance == null) return;
        DayManager.Instance.OnDayEnded += ShowPanel;
        _subscribed = true;
    }

    // Con el mismo patrón de reintento que DayReport: el InputManager puede
    // aparecer más tarde (orden de ejecución).
    private void TrySubscribeSkip()
    {
        if (_skipSubscribed || InputManager.Instance == null) return;
        InputManager.Instance.OnInteractPressed += OnInteractPressedSkip;
        _skipSubscribed = true;
    }

    /// <summary>Muestra el overlay y lanza la entrada del ticket. Lo dispara
    /// OnDayEnded. Antes sale la cinemática de cierre (subida de cámara +
    /// anochecer, en DayReport): si puede lanzarla, el panel espera a que la
    /// llame ella.</summary>
    public void ShowPanel()
    {
        DayReport report = DayReport.Instance;
        if (report != null && report.TryPlayEndOfDayCinematic(ShowPanelNow))
            return;

        ShowPanelNow();
    }

    private void ShowPanelNow()
    {
        StopSequence();
        CollectPrintLines();
        PrepareEmptyTicket();

        ShowRoot(true);
        _dailyStatsPanel?.SetActive(true);
        if (_nextDayButtonRect != null)
            _nextDayButtonRect.anchoredPosition3D = _nextDayButtonDefaultPos;

        // Coloca el ticket fuera de pantalla, por arriba, ya activado y en la
        // misma frame: nunca se llega a ver en su posición final.
        bool canDrop = PositionAboveScreen();

        // Congelar como en el menú de pausa: no debes poder moverte ni
        // interactuar mientras sale el ticket.
        Time.timeScale = 0f;
        if (InputManager.Instance != null)
            InputManager.Instance.EnterPause();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayStatsMusic();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("day_end");

        _sequenceStartFrame = Time.frameCount;
        _sequenceRoutine = StartCoroutine(EntranceRoutine(canDrop));
    }

    // --- Secuencia de entrada: bajada + impresión ---

    private IEnumerator EntranceRoutine(bool drop)
    {
        if (drop)
            yield return DropRoutine();

        yield return TypeLinesRoutine();
        FinishSequence();
    }

    /// <summary>Baja el ticket a tirones de impresora: cada tramo recorre una
    /// fracción de la distancia total (30%, luego 20%, ...), con un parón
    /// entre tramo y tramo y un pequeño sobretiro al aterrizar cada uno.</summary>
    private IEnumerator DropRoutine()
    {
        if (_panelRect == null) yield break;

        float startY = _panelRect.anchoredPosition.y;
        float total = startY - _panelHomeAnchoredPos.y;
        if (total <= 0.01f) yield break;

        float[] fracs = NormalizedDropSteps();
        float cumulative = 0f;

        for (int i = 0; i < fracs.Length; i++)
        {
            float frac = fracs[i];
            bool last = i >= fracs.Length - 1;
            if (last) frac = 1f - cumulative; // el último tramo aterriza el ticket
            cumulative += frac;

            float targetY = startY - total * cumulative;
            float overshoot = total * frac * _dropRecoilPercent;

            PlaySfx(_dropSfx);
            yield return MovePanelY(targetY - overshoot, _dropStepSeconds);
            if (overshoot > 0.01f && _dropRecoilSeconds > 0f)
                yield return MovePanelY(targetY, _dropRecoilSeconds);

            if (!last && _dropStepPause > 0f)
                yield return new WaitForSecondsRealtime(_dropStepPause);
        }

        SetPanelY(_panelHomeAnchoredPos.y);
    }

    /// <summary>Tramos de bajada normalizados (suman 1). Con la lista vacía o
    /// toda a cero, un único tramo.</summary>
    private float[] NormalizedDropSteps()
    {
        if (_dropStepPercents == null || _dropStepPercents.Length == 0)
            return new[] { 1f };

        float sum = 0f;
        foreach (float p in _dropStepPercents) sum += Mathf.Max(0f, p);
        if (sum <= 0f) return new[] { 1f };

        float[] fracs = new float[_dropStepPercents.Length];
        for (int i = 0; i < fracs.Length; i++)
            fracs[i] = Mathf.Max(0f, _dropStepPercents[i]) / sum;
        return fracs;
    }

    private IEnumerator MovePanelY(float targetY, float seconds)
    {
        if (_panelRect == null) yield break;

        float from = _panelRect.anchoredPosition.y;
        if (seconds <= 0f)
        {
            SetPanelY(targetY);
            yield break;
        }

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            k = 1f - (1f - k) * (1f - k); // easeOutQuad: tirón seco y parón
            SetPanelY(Mathf.LerpUnclamped(from, targetY, k));
            if (k >= 1f) break;
            yield return null;
        }
        SetPanelY(targetY);
    }

    private void SetPanelY(float y)
    {
        if (_panelRect != null)
            _panelRect.anchoredPosition = new Vector2(_panelHomeAnchoredPos.x, y);
    }

    /// <summary>Coloca el ticket fuera de la pantalla por arriba. Devuelve
    /// false si no se puede mover (p. ej. es el RectTransform de un canvas
    /// raíz, que Unity maneja él solo): en ese caso la entrada se limita a
    /// la impresión de valores.</summary>
    private bool PositionAboveScreen()
    {
        if (_panelRect == null) return false;
        if (!(_panelRect.transform.parent is RectTransform)) return false;

        SetPanelY(_panelHomeAnchoredPos.y + ComputeHiddenOffset());
        return true;
    }

    /// <summary>Distancia (en unidades de anchoredPosition) hasta dejar el
    /// ticket entero por encima del canvas raíz. Se mide con esquinas en
    /// mundo para que valga con cualquier ancla, pivote o escala.</summary>
    private float ComputeHiddenOffset()
    {
        float neededWorldUp;
        Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;

        if (canvasRect != null)
        {
            canvasRect.GetWorldCorners(_worldCorners);
            float canvasTopY = _worldCorners[1].y;
            _panelRect.GetWorldCorners(_worldCorners);
            float panelBottomY = _worldCorners[0].y;
            neededWorldUp = canvasTopY - panelBottomY + _dropStartMargin;
        }
        else
        {
            neededWorldUp = _panelRect.rect.height * 1.2f + _dropStartMargin;
        }

        // Unidades de anchoredPosition -> unidades de mundo del padre.
        float unitsPerAnchored = 1f;
        Transform parent = _panelRect.parent;
        if (parent != null)
        {
            float s = Mathf.Abs(parent.lossyScale.y);
            if (s > 1e-5f) unitsPerAnchored = s;
        }
        return neededWorldUp / unitsPerAnchored;
    }

    /// <summary>La impresora rellena los valores: línea a línea, carácter a
    /// carácter; la nota se sella al llegar a su sitio.</summary>
    private IEnumerator TypeLinesRoutine()
    {
        foreach (PrintLine line in _printLines)
        {
            if (line.isGrade)
            {
                yield return StampGradeRoutine();
                continue;
            }

            if (line.target == null) continue;

            yield return TypeLineRoutine(line);
            if (line.moneyLine)
                PlaySfx(_moneySfx);

            if (_typeLinePause > 0f)
                yield return new WaitForSecondsRealtime(_typeLinePause);
        }
    }

    private IEnumerator TypeLineRoutine(PrintLine line)
    {
        if (line.target == null) yield break;

        string final = line.finalText ?? string.Empty;
        if (final.Length == 0) yield break;

        string cursor = _showTypeCursor ? "_" : string.Empty;
        int shown = 0;
        while (shown < final.Length)
        {
            shown++;
            bool done = shown >= final.Length;
            line.target.text = final.Substring(0, shown) + (done ? string.Empty : cursor);
            PlaySfx(_typeSfx);

            if (done) break;

            float t = 0f;
            while (t < _typeCharSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        line.target.text = final;
    }

    /// <summary>La nota no se teclea: cae sellada (escala 1.5 -> 1).</summary>
    private IEnumerator StampGradeRoutine()
    {
        if (_gradeImage == null) yield break;

        _gradeImage.enabled = true;
        Transform stamp = _gradeImage.transform;
        if (_stampSeconds > 0f)
        {
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _stampSeconds);
                k *= k; // easeIn: el sello cae y golpea
                stamp.localScale = Vector3.LerpUnclamped(_gradeHomeScale * 1.5f, _gradeHomeScale, k);
                if (k >= 1f) break;
                yield return null;
            }
        }
        stamp.localScale = _gradeHomeScale;
        PlaySfx(_stampSfx);

        if (_typeLinePause > 0f)
            yield return new WaitForSecondsRealtime(_typeLinePause);
    }

    /// <summary>Deja la secuencia como estaría al terminar: ticket en su
    /// sitio, todos los valores impresos, nota visible y botón activo.</summary>
    private void FinishSequence()
    {
        _sequenceRoutine = null;

        if (_panelRect != null)
            _panelRect.anchoredPosition = _panelHomeAnchoredPos;

        ApplyFinalValues();

        if (_nextDayButton != null)
            _nextDayButton.interactable = true;
        SetupNextDayButton(); // restaura etiqueta y listeners tras la etiqueta de "imprimiendo"
    }

    private void ApplyFinalValues()
    {
        foreach (PrintLine line in _printLines)
        {
            if (line.isGrade)
            {
                if (_gradeImage != null)
                {
                    _gradeImage.enabled = true;
                    _gradeImage.transform.localScale = _gradeHomeScale;
                }
                continue;
            }

            if (line.target != null)
                line.target.text = line.finalText;
        }
    }

    private void StopSequence()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    /// <summary>E durante la entrada: completa al instante la bajada y la
    /// impresión (SkipSequence).</summary>
    private void OnInteractPressedSkip()
    {
        if (_sequenceRoutine == null) return;
        if (DayReport.Instance != null && DayReport.Instance.IsCinematicPlaying) return;
        // El pulso E que salta la cinemática aterriza aquí en la misma frame
        // en la que arranca la entrada: ese pulso no debe saltarse también
        // la impresión, solo los siguientes.
        if (Time.frameCount <= _sequenceStartFrame) return;

        StopSequence();
        FinishSequence();
    }

    // --- Preparación del ticket ---

    /// <summary>Recopila las líneas que imprimirá el ticket con sus textos
    /// finales (los mismos valores que rellenaba Populate).</summary>
    private void CollectPrintLines()
    {
        _printLines.Clear();

        if (_dayNumberText != null && SaveManager.Instance != null)
        {
            int playingDay = SaveManager.Instance.CurrentDay + 1;
            AddLine(_dayNumberText, $"DÍA {playingDay} · {WeekManager.GetDayName(playingDay)}");
        }

        DayReport report = DayReport.Instance;
        if (report == null)
            report = FindAnyObjectByType<DayReport>();

        if (report != null)
        {
            AddLine(_dishesServedText, $"{report.DishesServed}");
            AddLine(_moneyEarnedText, $"+{report.MoneyEarned}€");
            AddLine(_moneySpentText, $"-{report.MoneySpent}€");

            AddLine(_clientsSatisfiedText, $"{report.ClientsSatisfied}/{report.TotalClients}");

            if (_gradeImage != null)
                _gradeImage.sprite = GetGradeSprite(report.GetGrade());
        }
        else
        {
            // Sin informe no hay datos del día: se imprimen ceros para que
            // nunca se vean los textos de la escena como valores.
            Debug.LogWarning("[StatsPanel] No hay DayReport; el informe saldrá a cero.", this);
            AddLine(_dishesServedText, "0");
            AddLine(_moneyEarnedText, "+0€");
            AddLine(_moneySpentText, "-0€");
            AddLine(_clientsSatisfiedText, "0/0");
        }

        if (_balanceText != null && MoneyManager.Instance != null)
            AddLine(_balanceText, $"{MoneyManager.Instance.CurrentMoney}€", moneyLine: true);

        // El dinero neto cierra el ticket: ganado - gastado, ya con la cartera vista.
        if (report != null)
        {
            int net = report.NetMoney;
            AddLine(_netMoneyText, net >= 0 ? $"+{net}€" : $"{net}€", moneyLine: true);
        }
        else
        {
            AddLine(_netMoneyText, "+0€", moneyLine: true);
        }

        if (_gradeImage != null)
            _printLines.Add(new PrintLine { isGrade = true });
    }

    private void AddLine(TMP_Text target, string finalText, bool moneyLine = false)
    {
        if (target == null) return;
        _printLines.Add(new PrintLine { target = target, finalText = finalText, moneyLine = moneyLine });
    }

    /// <summary>Deja el ticket recién colgado: etiquetas visibles, VALORES
    /// vacíos, nota sin sellar y botón todavía fuera de juego.</summary>
    private void PrepareEmptyTicket()
    {
        foreach (PrintLine line in _printLines)
        {
            if (line.isGrade) continue;
            if (line.target != null)
                line.target.text = string.Empty;
        }

        if (_gradeImage != null)
        {
            _gradeImage.enabled = false;
            _gradeImage.transform.localScale = _gradeHomeScale;
        }

        PopulateWeekSection();

        if (_nextDayButton != null)
            _nextDayButton.interactable = false;
        SetupNextDayButton();
        if (!string.IsNullOrEmpty(_printingButtonLabel) && _nextDayButtonLabel != null)
            _nextDayButtonLabel.text = _printingButtonLabel;
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

        SetStars(r.starsBefore, r.starsAfter);

        if (_weekBonusText != null)
            _weekBonusText.text = r.moneyBonus > 0 ? $"BONUS: +{r.moneyBonus}€" : string.Empty;
    }

    private void SetStars(float starsBefore, float starsAfter)
    {
        if (_starImages == null) return;

        for (int i = 0; i < _starImages.Length; i++)
        {
            if (_starImages[i] == null) continue;

            float localBefore = Mathf.Clamp(starsBefore - i, 0f, 1f);
            float localAfter  = Mathf.Clamp(starsAfter - i, 0f, 1f);

            _starImages[i].sprite = GetStarSprite(localBefore, localAfter);
        }
    }

    private Sprite GetStarSprite(float localBefore, float localAfter)
    {
        if (localBefore == 0.5f && localAfter == 1f) return _halfGoldGreenSprite;
        if (localBefore == 1f && localAfter == 0.5f) return _halfGoldRedSprite;

        StarColorSprites set = localAfter > localBefore ? _greenStars
                            : localAfter < localBefore ? _redStars
                            : _goldStars;

        if (localAfter >= 1f) return set.full;
        if (localAfter >= 0.5f) return set.half;
        return set.empty;
    }

    private void OnViewWeekSummaryButton()
    {
        _awaitingWeekSummaryTap = false;
        if (_dailyStatsPanel != null) _dailyStatsPanel.SetActive(false);
        if (_weekResultRoot != null) _weekResultRoot.SetActive(true);
        if (_nextDayButtonLabel != null) _nextDayButtonLabel.text = "SIGUIENTE SEMANA";
        if (_nextDayButtonRect != null) _nextDayButtonRect.anchoredPosition3D = WeekSummaryButtonPos; // ← nuevo

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
        return grade switch
        {
            'A' => _gradeASprite,
            'B' => _gradeBSprite,
            'C' => _gradeCSprite,
            'D' => _gradeDSprite,
            'E' => _gradeESprite,
            _ => _gradeFSprite,
        };
    }

    private void PlaySfx(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(name);
    }

    /// <summary>
    /// Botón "Siguiente día"
    /// </summary>
    public void OnNextDayButton()
    {
        // Descongelar ANTES de cambiar de escena, o la siguiente carga parada.
        Time.timeScale = 1f;
        if (InputManager.Instance != null)
            InputManager.Instance.ExitPause();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("next_day");
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

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

    private void OnDestroy()
    {
        // Si la escena se descarga con el panel abierto, la siguiente arrancaría
        // congelada.
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
            if (InputManager.Instance != null)
                InputManager.Instance.ExitPause();
        }
    }

    [ContextMenu("DEBUG: Reproducir la entrada del ticket (bajada + impresión)")]
    private void DebugPlayEntrance() => ShowPanelNow();

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
            if (WeekManager.Instance != null)
                WeekManager.Instance.OnDayCompleted();
            SaveManager.Instance.IncrementDayAndSave();
            if (DayReport.Instance != null)
                DayReport.Instance.ResetCounters();
        }

        // Congela lo que quede de día detrás del overlay; el botón del final
        // devuelve al menú y reactiva el tiempo.
        Time.timeScale = 0f;
        ShowRoot(false);
        GameOverScreen.Show();
    }
}
