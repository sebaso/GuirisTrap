using UnityEngine;
using System.Collections.Generic;


public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale = 1f;
    }

    [Header("Fuentes de audio")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Biblioteca de efectos")]
    [SerializeField] private NamedClip[] _sfxLibrary;

    [Header("Música por escena")]
    [SerializeField] private AudioClip _prepMusic;
    [SerializeField] private AudioClip _gameMusic;
    [SerializeField] private AudioClip _menuMusic;
    [Tooltip("Música/fanfarria de la pantalla de estadísticas de fin de día.")]
    [SerializeField] private AudioClip _statsMusic;

    [Header("Volúmenes")]
    [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume   = 1f;

    private Dictionary<string, NamedClip> _sfxLookup;

    
    private const string PREF_MUSIC = "vol_music";
    private const string PREF_SFX   = "vol_sfx";

    public float MusicVolume => _musicVolume;
    public float SfxVolume   => _sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        
        BuildSfxLookup();

        // Cargar volúmenes guardados.
        _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC, _musicVolume);
        _sfxVolume   = PlayerPrefs.GetFloat(PREF_SFX,   _sfxVolume);
        ApplyVolumes();
    }

    private void BuildSfxLookup()
    {
        _sfxLookup = new Dictionary<string, NamedClip>();
        if (_sfxLibrary == null) return;

        foreach (NamedClip nc in _sfxLibrary)
        {
            if (nc == null || string.IsNullOrEmpty(nc.name) || nc.clip == null) continue;
            if (!_sfxLookup.ContainsKey(nc.name))
                _sfxLookup.Add(nc.name, nc);
            else
                Debug.LogWarning($"[AudioManager] Efecto duplicado: '{nc.name}'. Se ignora la copia.");
        }
    }


    public void PlaySFX(string name)
    {
        if (_sfxLookup == null || !_sfxLookup.TryGetValue(name, out NamedClip nc))
        {
            Debug.LogWarning($"[AudioManager] No existe el efecto '{name}'.");
            return;
        }
        _sfxSource.PlayOneShot(nc.clip, _sfxVolume * nc.volumeScale);
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume * volumeScale);
    }


    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        _musicSource.loop = loop;
        _musicSource.volume = _musicVolume;
        _musicSource.Play();
    }


    public void PlayMenuMusic() => PlayMusic(_menuMusic);
    public void PlayPrepMusic() => PlayMusic(_prepMusic);
    public void PlayGameMusic() => PlayMusic(_gameMusic);
    public void PlayStatsMusic() => PlayMusic(_statsMusic, false); // fanfarria, sin loop

    public void StopMusic() => _musicSource.Stop();



    public void SetMusicVolume(float value)
    {
        _musicVolume = Mathf.Clamp01(value);
        _musicSource.volume = _musicVolume;
        PlayerPrefs.SetFloat(PREF_MUSIC, _musicVolume);
    }

    public void SetSfxVolume(float value)
    {
        _sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_SFX, _sfxVolume);
    }

    private void ApplyVolumes()
    {
        if (_musicSource != null) _musicSource.volume = _musicVolume;
    }
}