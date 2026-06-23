using UnityEngine;


public class SceneMusic : MonoBehaviour
{
    public enum Which { Menu, Preparation, Game }

    [SerializeField] private Which _music = Which.Game;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusic] No hay AudioManager. ¿Está en la primera escena (Menú)?");
            return;
        }

        switch (_music)
        {
            case Which.Menu:        AudioManager.Instance.PlayMenuMusic(); break;
            case Which.Preparation: AudioManager.Instance.PlayPrepMusic(); break;
            case Which.Game:        AudioManager.Instance.PlayGameMusic(); break;
        }
    }
}
