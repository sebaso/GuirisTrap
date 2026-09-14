using UnityEditor;
using UnityEngine;

/// <summary>
/// Toggle de editor para el "modo showcase" del ciclo día/noche: un barrido rápido
/// del día pensado para grabar material de marketing (tráilers, GIFs, capturas).
/// Solo existe en el Editor; en builds el flag de runtime nunca se activa.
/// El barrido solo corre en Play Mode (es un MonoBehaviour normal, sin ExecuteAlways).
/// </summary>
public static class DayNightShowcaseMenu
{
    private const string MenuPath = "Tools/Modo showcase (ciclo dia/noche)";

    // SessionState sobrevive a los domain reloads (p.ej. un recompile en medio del
    // Play) pero se resetea al cerrar el Editor: alcance de sesión, como debe ser.
    private const string SessionKey = "DayNightCycle.ShowcaseMode";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Set(!DayNightCycle.ShowcaseMode);
    }

    private static void Set(bool value)
    {
        DayNightCycle.ShowcaseMode = value;
        SessionState.SetBool(SessionKey, value);
        Menu.SetChecked(MenuPath, value);

        Debug.Log(value
            ? "[DayNightCycle] Modo showcase ACTIVADO: el ciclo recorre el día completo " +
              "en bucle. Entra en Play y graba la Game View (solo afecta a la iluminación)."
            : "[DayNightCycle] Modo showcase desactivado: el ciclo vuelve a seguir el día real.");
    }

    // Tras cada recarga de dominio: reflejar el estado en el menú y re-sincronizar
    // el flag de runtime (los statics se resetean con la recarga, SessionState no).
    [InitializeOnLoadMethod]
    private static void Restore()
    {
        DayNightCycle.ShowcaseMode = SessionState.GetBool(SessionKey, false);
        Menu.SetChecked(MenuPath, DayNightCycle.ShowcaseMode);
    }
}
