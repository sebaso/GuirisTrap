using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla final de la demo: se construye a sí misma (canvas, fondos, textos y
// botón) al mostrarse, así no hay que montar UI en escena. Muestra las
// estadísticas combinadas de todos los días jugados y vuelve al menú.
public class GameOverScreen : MonoBehaviour
{
    public static void Show()
    {
        if (FindAnyObjectByType<GameOverScreen>() != null) return;
        new GameObject("GameOverScreen").AddComponent<GameOverScreen>();
    }

    private void Awake()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform bg = CreateRect(canvasGo.transform, "Fondo", Vector2.zero, Vector2.one);
        bg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        TextMeshProUGUI title = CreateText(canvasGo.transform, "Título", "¡SEMANA COMPLETADA!", 72,
            new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(1400, 120));
        title.color = new Color(1f, 0.85f, 0.3f);

        TextMeshProUGUI subtitle = CreateText(canvasGo.transform, "Subtítulo", "Fin de la demo · resumen de todos los días", 32,
            new Vector2(0.5f, 1f), new Vector2(0, -225), new Vector2(1400, 60));
        subtitle.color = new Color(0.8f, 0.8f, 0.8f);

        TextMeshProUGUI stats = CreateText(canvasGo.transform, "Stats", "", 40,
            new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(900, 480));
        stats.text = BuildStatsText();
        stats.lineSpacing = 1.35f;

        Button menuButton = CreateButton(canvasGo.transform, "VOLVER AL MENÚ", new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(420, 90));
        menuButton.onClick.AddListener(BackToMenu);
    }

    private string BuildStatsText()
    {
        var stats = SaveManager.Instance != null ? SaveManager.Instance.WeekDayStats : null;
        if (stats == null || stats.Count == 0)
            return "Sin datos de la semana.";

        int dishes = stats.Sum(s => s.dishes);
        int satisfied = stats.Sum(s => s.satisfied);
        int angry = stats.Sum(s => s.angry);
        int earned = stats.Sum(s => s.earned);
        int total = satisfied + angry;
        float satisfaction = total > 0 ? satisfied * 100f / total : 0f;
        float avgScore = stats.Average(s => (float)s.score);
        char avgGrade = WeekManager.ScoreToGrade(Mathf.FloorToInt(avgScore + 0.5f));
        float stars = SaveManager.Instance.Stars;

        return $"Días jugados: {stats.Count}\n" +
               $"Platos servidos: {dishes}\n" +
               $"Clientes satisfechos: {satisfied}   ·   Enfadados: {angry}\n" +
               $"Satisfacción: {satisfaction:0}%\n" +
               $"Nota media: {avgGrade} ({avgScore:0.00})\n" +
               $"Dinero ganado: {earned} €\n" +
               $"Estrellas finales: {stars:0.##} / 5";
    }

    private void BackToMenu()
    {
        // Checkpoint final para que "Continuar" muestre la partida completa.
        if (SaveManager.Instance != null)
            SaveManager.Instance.ForceSave();
        Time.timeScale = 1f;
        if (SceneController.Instance != null)
            SceneController.Instance.ChangeScene("MainMenu");
    }

    // ── construcción de UI ────────────────────────────────────────────────

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
        Vector2 anchor, Vector2 position, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = sizeDelta;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 sizeDelta)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = sizeDelta;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.5f, 0.25f);

        Button button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.25f, 0.65f, 0.33f);
        colors.pressedColor = new Color(0.12f, 0.35f, 0.18f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, 34,
            new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta);
        // estirar el label sobre todo el botón; con anchors de punto y offsets a
        // cero el rect colapsa a ancho 0 y TMP parte el texto letra a letra
        RectTransform labelRect = text.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }
}
