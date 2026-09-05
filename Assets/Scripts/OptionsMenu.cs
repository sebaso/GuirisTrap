using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class OptionsMenu : MonoBehaviour
{
    [Header("Pantalla completa ")]
    [SerializeField] private Toggle _fullscreenToggle;

    [Header("Pantalla completa")]
    [SerializeField] private Button _fullscreenButton;
    [SerializeField] private TextMeshProUGUI _fullscreenLabel;
    [SerializeField] private string _labelOn  = "Pantalla completa:  SÍ";
    [SerializeField] private string _labelOff = "Pantalla completa:  NO";

    private const string PREF_FULLSCREEN = "opt_fullscreen";


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettingsOnStartup()
    {
        if (!PlayerPrefs.HasKey(PREF_FULLSCREEN)) return;
        Screen.fullScreen = PlayerPrefs.GetInt(PREF_FULLSCREEN) == 1;
    }

    void Awake()
    {
        bool full = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = full;

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.SetIsOnWithoutNotify(full);
            _fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (_fullscreenButton != null)
            _fullscreenButton.onClick.AddListener(ToggleFullscreen);

        RefreshLabel(full);
    }

    void OnEnable()
    {
        // Por si alguien cambió la ventana por fuera (alt+enter) mientras el
        // menú estaba cerrado.
        bool full = Screen.fullScreen;
        _fullscreenToggle?.SetIsOnWithoutNotify(full);
        RefreshLabel(full);
    }


    public void ToggleFullscreen() => SetFullscreen(!Screen.fullScreen);

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;

        PlayerPrefs.SetInt(PREF_FULLSCREEN, value ? 1 : 0);
        PlayerPrefs.Save();

        _fullscreenToggle?.SetIsOnWithoutNotify(value);
        RefreshLabel(value);
    }

    private void RefreshLabel(bool full)
    {
        if (_fullscreenLabel != null)
            _fullscreenLabel.text = full ? _labelOn : _labelOff;
    }

    /// <summary>Engánchalo al On Click () del botón de volver.</summary>
    public void Back()
    {
        if (PauseMenuManager.Instance != null)
            PauseMenuManager.Instance.CloseOptions();
        else
            gameObject.SetActive(false); // en el menú principal no hay pausa
    }
}