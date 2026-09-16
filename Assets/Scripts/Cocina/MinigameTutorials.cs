using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class MinigameTutorials : MonoBehaviour
{
    [Serializable]
    public class Entrada
    {
        public MinigameType tipo;

        [Tooltip("Título grande. Ej: EL MORTERO")]
        public string titulo = "MINIJUEGO";

        [Tooltip("Las instrucciones. Se admiten saltos de línea.")]
        [TextArea(3, 8)]
        public string texto = "";

        [Tooltip("Opcional: una imagen de apoyo (los controles, por ejemplo).")]
        public Sprite imagen;

        [Tooltip("Segundos antes de poder cerrarla. A 0 se puede cerrar al instante.")]
        public float segundosMinimos = 1.0f;
    }

    [Header("Panel")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _tituloText;
    [SerializeField] private TMP_Text _cuerpoText;
    [SerializeField] private Image _imagen;
    [Tooltip("Texto tipo 'Pulsa E para empezar' / la cuenta atrás.")]
    [SerializeField] private TMP_Text _pieText;

    [Header("Contenido")]
    [SerializeField] private Entrada[] _entradas;

    [Header("Comportamiento")]
    [Tooltip("Desmárcalo para que salgan SIEMPRE. Útil mientras se prueba.")]
    [SerializeField] private bool _soloUnaVez = true;
    [Tooltip("Texto del pie cuando ya se puede cerrar.")]
    [SerializeField] private string _textoContinuar = "Pulsa E para empezar";

    private const string PrefKey = "tuto_minijuego_";

    private Action _alCerrar;
    private bool _abierta;
    private float _puedeCerrarseEn;

    public static MinigameTutorials Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (_panel != null) _panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!_abierta) return;

        float restante = _puedeCerrarseEn - Time.unscaledTime;

        if (_pieText != null)
        {
            _pieText.text = restante > 0f
                ? Mathf.CeilToInt(restante).ToString()
                : _textoContinuar;
        }

        if (restante <= 0f && HayPulsacion()) Cerrar();
    }

    private bool HayPulsacion()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var gp = UnityEngine.InputSystem.Gamepad.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;

        if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame ||
                           gp.buttonEast.wasPressedThisFrame ||
                           gp.startButton.wasPressedThisFrame)) return true;

        return false;
    }


    /// <summary>¿Hay que explicar este minijuego antes de lanzarlo?</summary>
    public bool HaceFalta(MinigameType tipo)
    {
        if (_panel == null) return false;
        if (Buscar(tipo) == null) return false;
        if (!_soloUnaVez) return true;

        return PlayerPrefs.GetInt(PrefKey + tipo, 0) == 0;
    }

    /// <summary>Muestra la explicación y llama a 'alCerrar' cuando el jugador la
    /// cierra. Si no hay nada que explicar, llama a 'alCerrar' al momento.</summary>
    public void Mostrar(MinigameType tipo, Action alCerrar)
    {
        Entrada e = Buscar(tipo);

        if (e == null || _panel == null)
        {
            alCerrar?.Invoke();
            return;
        }

        _alCerrar = alCerrar;
        _abierta = true;
        _puedeCerrarseEn = Time.unscaledTime + Mathf.Max(0f, e.segundosMinimos);

        if (_tituloText != null) _tituloText.text = e.titulo;
        if (_cuerpoText != null) _cuerpoText.text = e.texto;

        if (_imagen != null)
        {
            _imagen.sprite = e.imagen;
            _imagen.gameObject.SetActive(e.imagen != null);
        }

        _panel.SetActive(true);

        Time.timeScale = 0f;

        if (_soloUnaVez) PlayerPrefs.SetInt(PrefKey + tipo, 1);
        PlayerPrefs.Save();
    }

    private void Cerrar()
    {
        _abierta = false;
        if (_panel != null) _panel.SetActive(false);

        Time.timeScale = 1f;

        Action cb = _alCerrar;
        _alCerrar = null;
        cb?.Invoke();
    }

    private Entrada Buscar(MinigameType tipo)
    {
        if (_entradas == null) return null;

        foreach (Entrada e in _entradas)
            if (e != null && e.tipo == tipo) return e;
        return null;
    }

    [ContextMenu("DEBUG: olvidar todas las explicaciones")]
    public void ReiniciarTutoriales()
    {
        foreach (MinigameType t in Enum.GetValues(typeof(MinigameType)))
            PlayerPrefs.DeleteKey(PrefKey + t);

        PlayerPrefs.Save();
        Debug.Log("[MinigameTutorials] Explicaciones reiniciadas: volverán a salir.");
    }
}
