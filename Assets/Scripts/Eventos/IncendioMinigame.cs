using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MINIJUEGO DE APAGAR INCENDIOS (GDD):
///
/// - SIN extintor (difícil): soplar → alternar A-D (←/→) muy rápido para
///   llenar la barra. El progreso DECAE con el tiempo. Límite 10s (GDD).
///
/// - CON extintor (muy fácil): pulsar E unas pocas veces, sin decaimiento.
///   El extintor es el objeto FÍSICO que se coge de su soporte
///   (ExtintorPickup). Es de UN SOLO USO por viaje: al terminar este
///   minijuego (se gane o se pierda), el extintor vuelve a su soporte.
///
/// Si se falla, el fuego SIGUE ardiendo y se puede reintentar interactuando
/// otra vez con la estación (pero si usaste el extintor, tendrás que ir a por
/// él de nuevo → el reintento sería a soplo limpio).
///
/// SETUP (mismo patrón que DespensaMinigame):
///   1. Añadir este script al objeto SistemasMinijuegos.
///   2. Panel en el Canvas (duplica el de Despensa): barra de progreso
///      (Image fill), texto de instrucción y texto de timer.
///   3. Arrastrar el panel/refs, y este script al FireEventManager.
/// </summary>
public class IncendioMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public Image progressBarFill;
    public TMP_Text instructionText;
    public TMP_Text timerText;

    [Header("Sin extintor (soplar, difícil)")]
    [Tooltip("Segundos para apagar el fuego soplando. Métrica GDD: 10s.")]
    public float hardTimeLimit = 10f;
    [Tooltip("Alternancias A-D necesarias para llenar la barra.")]
    public int hardHitsRequired = 20;
    [Tooltip("Golpes de progreso que se pierden por segundo si dejas de soplar.")]
    public float hardDecayPerSecond = 1.5f;

    [Header("Con extintor (muy fácil)")]
    public float easyTimeLimit = 10f;
    [Tooltip("Pulsaciones de E necesarias con el extintor.")]
    public int easyPressesRequired = 4;

    private CookingStation   _station;
    private PlayerController _player;
    private bool  _isPlaying = false;
    private bool  _easyMode  = false;
    private bool  _usedExtintor = false;   // para devolverlo al soporte al terminar
    private float _timer;
    private float _maxTimer;
    private float _progress;      // en "golpes" (float por el decaimiento)
    private float _required;
    private int   _lastBlowSign;  // -1 = izquierda, +1 = derecha, 0 = aún nada

    void Awake()
    {
        if (minigamePanel) minigamePanel.SetActive(false);
    }

    /// <summary>Lo lanza FireEventManager cuando interactúas con una estación en llamas.</summary>
    public void StartMinigame(CookingStation station, PlayerController currentPlayer)
    {
        _station = station;
        _player  = currentPlayer;

        // Modo fácil solo si el jugador LLEVA el extintor físico encima
        // (lo cogió de su soporte). GDD: "debes tener a mano el extintor".
        _easyMode     = ExtintorPickup.IsPlayerCarrying;
        _usedExtintor = _easyMode;

        _required     = _easyMode ? easyPressesRequired : hardHitsRequired;
        _timer        = _easyMode ? easyTimeLimit       : hardTimeLimit;
        _maxTimer     = _timer;
        _progress     = 0f;
        _lastBlowSign = 0;

        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);

        if (progressBarFill) progressBarFill.fillAmount = 0f;
        if (instructionText)
            instructionText.text = _easyMode
                ? "¡EXTINTOR! Pulsa E para apagar el fuego"
                : "¡SOPLA! Alterna ← → (A-D) muy rápido";
        if (timerText) timerText.color = Color.white;

        _isPlaying = true;
    }

    void Update()
    {
        if (!_isPlaying) return;

        // Decaimiento: solo en modo difícil (hay que mantener el ritmo).
        if (!_easyMode && _progress > 0f)
        {
            _progress = Mathf.Max(0f, _progress - hardDecayPerSecond * Time.deltaTime);
            RefreshBar();
        }

        _timer -= Time.deltaTime;
        if (timerText)
        {
            timerText.text = _timer.ToString("F1");
            float ratio = Mathf.Clamp01(_timer / _maxTimer);
            timerText.color = ratio > 0.5f
                ? Color.Lerp(new Color(1f, 0.6f, 0f), Color.white, (ratio - 0.5f) * 2f)
                : Color.Lerp(Color.red, new Color(1f, 0.6f, 0f), ratio * 2f);
        }

        if (_timer <= 0f) EndGame(false);
    }

    // ------------------------------------------------------------------
    //  Progreso
    // ------------------------------------------------------------------

    private void Blow(int sign)
    {
        if (!_isPlaying || _easyMode) return;
        if (sign == _lastBlowSign) return; // mantener pulsado o repetir lado no cuenta
        _lastBlowSign = sign;
        AddProgress(1f);
    }

    private void ExtinguisherPress()
    {
        if (!_isPlaying || !_easyMode) return;
        AddProgress(1f);
    }

    private void AddProgress(float amount)
    {
        _progress += amount;
        RefreshBar();
        if (_progress >= _required) EndGame(true);
    }

    private void RefreshBar()
    {
        if (progressBarFill) progressBarFill.fillAmount = _progress / _required;
    }

    // ------------------------------------------------------------------
    //  Fin
    // ------------------------------------------------------------------

    void EndGame(bool success)
    {
        _isPlaying = false;
        minigamePanel.SetActive(false);
        if (timerText) timerText.color = Color.white;
        InputManager.Instance.ExitMinigame();

        // El extintor es de un solo uso por viaje: se devuelva o no el fuego,
        // vuelve a su soporte al terminar el minijuego.
        if (_usedExtintor && ExtintorPickup.Carried != null)
            ExtintorPickup.Carried.ReturnToHolder();

        if (success)
        {
            MinigameFeedback.Show(true, "¡Fuego apagado!", "incendio_success");
            FireEventManager.Instance?.Extinguish(_station);
        }
        else
        {
            // El fuego SIGUE ardiendo: se puede reintentar interactuando otra vez.
            MinigameFeedback.Show(false, "¡El fuego sigue ardiendo!", "incendio_failure");
        }

        _station = null;
        _usedExtintor = false;
    }

    //  IMinigameControllable

    public void OnNavigate(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) < 0.5f) return;
        Blow(direction.x > 0f ? 1 : -1);
    }

    public void OnInteract() => ExtinguisherPress();
    public void OnSubmit()   => ExtinguisherPress();
    public void OnCancel()   { }
}