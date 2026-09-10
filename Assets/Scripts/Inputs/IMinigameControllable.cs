using UnityEngine;

public interface IMinigameControllable
{
    void OnInteract();
    void OnNavigate(Vector2 direction);
    void OnCancel();
    void OnSubmit();
}