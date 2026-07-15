using UnityEngine;
using System.Collections.Generic;


public class WeekManager : MonoBehaviour
{
    public static WeekManager Instance { get; private set; }

    public const int DaysPerWeek = 6;

    [Header("Estrellas por media semanal")]
    [SerializeField] private float _deltaMediaA = 0.5f;
    [SerializeField] private float _deltaMediaB = 0.5f;
    [SerializeField] private float _deltaMediaC = 0.25f;
    [SerializeField] private float _deltaMediaD = 0f;
    [SerializeField] private float _deltaMediaE = -0.25f;
    [SerializeField] private float _deltaMediaF = -0.5f;

    [Header("Bonus de dinero por media de A")]
    [SerializeField] private int _mediaABonusMoney = 100;

    /// <summary>Resultado de la última semana cerrada (solo válido si WeekJustEnded).</summary>
    public WeekResult LastResult { get; private set; }

    /// <summary>True si el día que ACABA de terminar cerró una semana.</summary>
    public bool WeekJustEnded { get; private set; }

    private static readonly string[] DayNames =
        { "MARTES", "MIÉRCOLES", "JUEVES", "VIERNES", "SÁBADO", "DOMINGO" };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    /// <summary>
    /// Registra la nota del día que acaba de jugarse y, si con él se completa
    /// la semana, calcula la media y ajusta las estrellas.
    ///
    /// IMPORTANTE: debe llamarse ANTES de que se dispare OnDayEnded para que
    /// el StatsPanel pueda mostrar el resultado semanal al abrirse. No escribe
    /// a disco: DayManager.HandleDayEnd() llama a SaveManager.SaveMoney()
    /// justo después, que ya persiste todos los datos.
    /// </summary>
    public void OnDayCompleted()
    {
        WeekJustEnded = false;

        if (SaveManager.Instance == null || DayReport.Instance == null)
        {
            Debug.LogWarning("[WeekManager] Falta SaveManager o DayReport; no se registra la nota del día.");
            return;
        }

        // CurrentDay cuenta días COMPLETADOS (se incrementa al pulsar
        // "Siguiente día"), así que el día que acaba de jugarse es +1.
        int playingDay = SaveManager.Instance.CurrentDay + 1;
        int score      = GradeToScore(DayReport.Instance.GetGrade());

        List<int> grades = SaveManager.Instance.WeekGrades;

        // Si este día ya se había registrado (el jugador reinició el día sin
        // pasar al siguiente), sustituimos la última nota en vez de duplicarla.
        if (SaveManager.Instance.LastGradedDay == playingDay && grades.Count > 0)
            grades[grades.Count - 1] = score;
        else
            grades.Add(score);

        SaveManager.Instance.LastGradedDay = playingDay;

        Debug.Log($"[WeekManager] Día {playingDay} ({GetDayName(playingDay)}) → nota {ScoreToGrade(score)}. " +
                  $"Semana: {grades.Count}/{DaysPerWeek} días.");

        if (grades.Count >= DaysPerWeek)
            CloseWeek(playingDay);
    }

    private void CloseWeek(int playingDay)
    {
        List<int> grades = SaveManager.Instance.WeekGrades;

        float sum = 0f;
        foreach (int s in grades) sum += s;
        float avgScore = sum / grades.Count;

        // Redondeo estándar (2.5 → 3) para pasar la media numérica a letra.
        char avgGrade = ScoreToGrade(Mathf.FloorToInt(avgScore + 0.5f));

        float starsBefore = SaveManager.Instance.Stars;
        float starsAfter  = Mathf.Clamp(starsBefore + GetStarDelta(avgGrade), 0f, 5f);
        SaveManager.Instance.Stars = starsAfter;

        int bonus = 0;
        if (avgGrade == 'A' && _mediaABonusMoney > 0)
        {
            bonus = _mediaABonusMoney;
            MoneyManager.Instance?.AddMoney(bonus);
            DayReport.Instance?.RegisterEarnings(bonus);
        }

        grades.Clear();

        LastResult = new WeekResult
        {
            weekNumber   = (playingDay - 1) / DaysPerWeek + 1,
            averageScore = avgScore,
            averageGrade = avgGrade,
            starsBefore  = starsBefore,
            starsAfter   = starsAfter,
            moneyBonus   = bonus
        };
        WeekJustEnded = true;

        Debug.Log($"[WeekManager] FIN DE SEMANA {LastResult.weekNumber}: media {avgGrade} ({avgScore:0.00}) " +
                  $"→ estrellas {starsBefore:0.##} → {starsAfter:0.##}" +
                  (bonus > 0 ? $" + bonus {bonus}€" : ""));
    }

    private float GetStarDelta(char grade)
    {
        switch (grade)
        {
            case 'A': return _deltaMediaA;
            case 'B': return _deltaMediaB;
            case 'C': return _deltaMediaC;
            case 'D': return _deltaMediaD;
            case 'E': return _deltaMediaE;
            default:  return _deltaMediaF;
        }
    }

    /// <summary>A=5, B=4, C=3, D=2, E=1, F=0.</summary>
    public static int GradeToScore(char grade)
    {
        switch (grade)
        {
            case 'A': return 5;
            case 'B': return 4;
            case 'C': return 3;
            case 'D': return 2;
            case 'E': return 1;
            default:  return 0;
        }
    }

    /// <summary>5=A, 4=B, 3=C, 2=D, 1=E, 0=F.</summary>
    public static char ScoreToGrade(int score)
    {
        switch (Mathf.Clamp(score, 0, 5))
        {
            case 5:  return 'A';
            case 4:  return 'B';
            case 3:  return 'C';
            case 2:  return 'D';
            case 1:  return 'E';
            default: return 'F';
        }
    }

    /// <summary>Nombre del día de la semana para un día jugado (día 1 = MARTES).</summary>
    public static string GetDayName(int playingDay)
    {
        int index = ((playingDay - 1) % DaysPerWeek + DaysPerWeek) % DaysPerWeek;
        return DayNames[index];
    }
}

/// <summary>Resumen del cierre de una semana.</summary>
public struct WeekResult
{
    public int   weekNumber;
    public float averageScore;   // Media numérica (0-5).
    public char  averageGrade;   // Media redondeada a letra.
    public float starsBefore;
    public float starsAfter;
    public int   moneyBonus;

    public float StarsDelta => starsAfter - starsBefore;
}