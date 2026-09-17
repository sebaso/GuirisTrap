using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    private static bool _isQuitting;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_isQuitting)
            {
                Debug.LogWarning("CoroutineRunner: intento de acceso durante el cierre de la aplicación/escena, ignorado.");
                return null;
            }

            if (_instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                _instance = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        _isQuitting = true;
    }
}